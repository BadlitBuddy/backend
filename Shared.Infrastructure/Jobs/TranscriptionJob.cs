using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Abstractions.Jobs;
using Shared.Abstractions.Repositories;
using Shared.Abstractions.Services;
using Shared.Common.Helpers;
using Shared.Contracts.DtoMappers;
using Shared.Contracts.Enums;
using Shared.Infrastructure.Constants;
using Shared.Infrastructure.Services;
using TranscriptionJobStatus = Api.Domain.Enums.TranscriptionJobStatus;

namespace Shared.Infrastructure.Jobs;

public class TranscriptionJob : ITranscriptionJob
{
    private const long MaxLocalFileSizeToProcess = 24 * 1024 * 1024;

    private readonly IStreamingTranscriptionService _transcriptionService;
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly ILogger<TranscriptionJob> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ITranscriptRepository _transcriptRepository;
    private readonly IStreamableTranscriptionExporter _streamableTranscriptionExporter;
    private readonly ITranscriptionService _cloudFlareTranscriptionService;
    private readonly ITranscriptionService _groqTranscriptionService;
    private readonly WorkerOptions _workerOptions;

    public TranscriptionJob(
        IStreamingTranscriptionService transcriptionService, IAudioJobStorageService audioJobStorageService,
        ILogger<TranscriptionJob> logger, IHostEnvironment hostEnvironment, IMessagePublisher messagePublisher,
        ITranscriptRepository transcriptRepository, IOptions<WorkerOptions> workerOptions,
        IStreamableTranscriptionExporter streamableTranscriptionExporter,
        [FromKeyedServices(TranscriptionProvider.Cloudflare)]
        ITranscriptionService cloudFlareTranscriptionService,
        [FromKeyedServices(TranscriptionProvider.Groq)]
        ITranscriptionService groqTranscriptionService)
    {
        _transcriptionService = transcriptionService;
        _audioJobStorageService = audioJobStorageService;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _messagePublisher = messagePublisher;
        _transcriptRepository = transcriptRepository;
        _streamableTranscriptionExporter = streamableTranscriptionExporter;
        _cloudFlareTranscriptionService = cloudFlareTranscriptionService;
        _groqTranscriptionService = groqTranscriptionService;
        _workerOptions = workerOptions.Value;
    }

    public async Task TranscribeFileAsync(string fileKey, CancellationToken cancellationToken)
    {
        var isFileValid = await IsFileValid(fileKey, cancellationToken);
        if (!isFileValid)
        {
            throw new InvalidOperationException();
        }

        var (toProcessDir, processedDir) = CreateWorkingDirs(fileKey);
        var (toProcessFilePath, processedFilePath) = CreateWorkingFilePaths(toProcessDir, processedDir, fileKey);
        var (userId, originalFileName) = StoragePathBuilder.ExtractUnprocessedFileKeyParts(fileKey);

        string? outputObjectKey = null;
        try
        {
            _logger.LogInformation("Starting Whisper transcription for: {FileKey}", fileKey);

            var toProcessFileInfo = await DownloadAndSaveFileAsync(toProcessFilePath, fileKey, cancellationToken);

            switch (_workerOptions.TranscriptionProvider)
            {
                case TranscriptionProvider.WhisperNet:
                    await TranscribeWithWhisperNet(toProcessFilePath, processedFilePath, fileKey,
                        cancellationToken);
                    break;
                case TranscriptionProvider.Groq:
                    await TranscribeWithGroq(toProcessFileInfo, processedFilePath, processedDir, fileKey,
                        cancellationToken);
                    break;
                case TranscriptionProvider.Cloudflare:
                    await TranscribeWithCloudflare(toProcessFilePath, processedFilePath, fileKey, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException("Unknown transcription provider");
            }

            await using (var uploadStream = new FileStream(
                             processedFilePath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.Read,
                             bufferSize: 4096,
                             useAsync: true))
            {
                var originalFileNameJson = Path.ChangeExtension(originalFileName, ".json");
                var uploadResult = await _audioJobStorageService.UploadTranscriptionAsync(
                    userId,
                    originalFileNameJson,
                    uploadStream,
                    cancellationToken);

                outputObjectKey = uploadResult;
            }

            await _transcriptRepository.UpdateStatusAsync(fileKey, outputObjectKey,
                TranscriptionJobStatus.Completed, new Guid(userId));
            await _audioJobStorageService.DeleteAudioAsync(fileKey, cancellationToken);

            _logger.LogInformation("Finished transcription and cleanup for: {FileKey}", fileKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {FileKey}", fileKey);
            throw;
        }
        finally
        {
            DirectoryInfo directory = new DirectoryInfo(processedDir);
            if (directory.Exists)
            {
                foreach (FileInfo file in directory.EnumerateFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
                {
                    subDirectory.Delete(recursive: true);
                }
            }

            await _messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
                new TranscriptionProcessMessage(JobStatus.Finished, fileKey, outputObjectKey));
        }
    }

    [Queue(HangfireQueueConstants.WhisperTinyEn)]
    public async Task TranscribeFileWithWhisperTinyEnAsync(string fileKey, CancellationToken cancellationToken)
    {
        var isFileValid = await IsFileValid(fileKey, cancellationToken);
        if (!isFileValid)
        {
            throw new InvalidOperationException();
        }

        var (toProcessDir, processedDir) = CreateWorkingDirs(fileKey);
        var (toProcessFilePath, processedFilePath) = CreateWorkingFilePaths(toProcessDir, processedDir, fileKey);

        var (userId, originalFileName) = StoragePathBuilder.ExtractUnprocessedFileKeyParts(fileKey);

        string? outputObjectKey = null;
        try
        {
            _logger.LogInformation("Starting Whisper transcription for: {FileKey}", fileKey);

            var toProcessFileInfo = await DownloadAndSaveFileAsync(toProcessFilePath, fileKey, cancellationToken);

            await _transcriptRepository.UpdateStatusAsync(fileKey, null,
                TranscriptionJobStatus.Processing, new Guid(userId));

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation("Transcribing with CLoudflare Whisper Tiny-en with key: {FileKey}", fileKey);
            }

            using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var transcriptionTask =
                _cloudFlareTranscriptionService.TranscribeAsync(
                    new TranscriptionSource.FilePath(toProcessFileInfo),
                    TranscriptionModel.WhisperTinyEn,
                    cancellationToken);
            var progressTask = PublishProgressAsync(
                transcriptionTask,
                fileKey,
                progressCts.Token);

            TranscriptionResult transcriptionResult;
            try
            {
                transcriptionResult = await transcriptionTask;
            }
            finally
            {
                await progressCts.CancelAsync();
                await progressTask;
            }

            {
                await using var outPutFileStream = File.Create(processedFilePath);
                await _streamableTranscriptionExporter.ToJsonAsync(transcriptionResult, outPutFileStream,
                    cancellationToken);
            }

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation("Transcribed text: {Text}", transcriptionResult.Text);
            }

            await using (var uploadStream = new FileStream(
                             processedFilePath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.Read,
                             bufferSize: 4096,
                             useAsync: true))
            {
                var originalFileNameJson = Path.ChangeExtension(originalFileName, ".json");
                var uploadResult = await _audioJobStorageService.UploadTranscriptionAsync(
                    userId,
                    originalFileNameJson,
                    uploadStream,
                    cancellationToken);

                outputObjectKey = uploadResult;
            }

            _logger.LogInformation("Updating processed object key: {FileKey}", outputObjectKey);
            await _transcriptRepository.UpdateProcessedObjectKeyAsync(fileKey, outputObjectKey,
                TranscriptionJobStatus.Completed);
            await _audioJobStorageService.DeleteAudioAsync(fileKey, cancellationToken);

            _logger.LogInformation("Finished transcription and cleanup for: {FileKey}", fileKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {FileKey}", fileKey);
            throw;
        }
        finally
        {
            DirectoryInfo directory = new DirectoryInfo(processedDir);
            if (directory.Exists)
            {
                foreach (FileInfo file in directory.EnumerateFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
                {
                    subDirectory.Delete(recursive: true);
                }
            }

            await _messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
                new TranscriptionProcessMessage(JobStatus.Finished, fileKey, outputObjectKey));
        }
    }

    private async Task TranscribeWithWhisperNet(string toProcessFilePath, string outputFilePath, string fileKey,
        CancellationToken cancellationToken)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("Transcribing with Whisper.net");
        }

        await _messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
            new TranscriptionProcessMessage(JobStatus.Processing, fileKey, null));

        await using var stream = File.OpenRead(toProcessFilePath);
        var segments = new List<TranscriptionSegment>();

        await foreach (var segment in _transcriptionService.TranscribeStreamingAsync(stream, cancellationToken))
        {
            segments.Add(segment);

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation($"{segment.Start}->{segment.End}: {segment.Text}");
            }

            await _messagePublisher.PublishAsync(
                MessageChannel.TranscriptionProcess,
                new TranscriptionProcessMessage(JobStatus.Processing, fileKey, null));
        }

        TranscriptionResult transcriptionResult = segments.ToTranscriptionResult(
            task: TranscriptionTask.Transcribe,
            providerModelId: TranscriptionProvider.WhisperNet
        );

        await using var fileStream = File.Create(outputFilePath);
        await _streamableTranscriptionExporter.ToJsonAsync(transcriptionResult, fileStream, cancellationToken);
    }

    private async Task TranscribeWithGroq(FileInfo toProcessFile, string processedFilePath, string processedDir,
        string fileKey, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Transcribing with Groq");

        var isLargeFile = toProcessFile.Length > MaxLocalFileSizeToProcess;
        if (isLargeFile)
        {
            var toProcessFileFlac =
                await AudioFileProcessor.ConvertWavToFlacAsync(toProcessFile, processedDir, 5, cancellationToken);

            const long maxBytesPerChunk = 20 * 1024 * 1024;
            if (_hostEnvironment.IsDevelopment()) Console.WriteLine($"Max Bytes per chunk {maxBytesPerChunk} bytes");
            var chunkedFiles = await AudioFileProcessor.ChunkFileAsync(toProcessFileFlac,
                maxBytesPerChunk,
                processedDir, 3, cancellationToken);

            if (_hostEnvironment.IsDevelopment())
            {
                Array.ForEach(chunkedFiles,
                    file => _logger.LogInformation("Chunked File {FileName} has size {FileLength} bytes", file.Name,
                        file.Length));
            }

            using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            List<Task<TranscriptionResult>> transcriptionTasks = [];
            foreach (var chunkedFile in chunkedFiles)
            {
                _logger.LogInformation($"Transcribing File {chunkedFile.Name}");
                var transcriptionTask = _groqTranscriptionService.TranscribeAsync(
                    new TranscriptionSource.FilePath(chunkedFile),
                    TranscriptionModel.WhisperV3LargeTurbo,
                    progressCts.Token);
                transcriptionTasks.Add(transcriptionTask);
            }

            var tasks = Task.WhenAll(transcriptionTasks);
            var progressTask = PublishProgressAsync(
                tasks,
                fileKey,
                progressCts.Token);

            TranscriptionResult[] transcriptionResults;
            try
            {
                transcriptionResults = await tasks;
            }
            finally
            {
                await progressCts.CancelAsync();
                await progressTask;
            }

            List<FileInfo> chunkedJsonFiles = [];
            for (int i = 0; i < transcriptionResults.Length; i++)
            {
                string chunkedJsonFilePath = Path.Combine(processedDir, $"chunk-{i}.json");
                FileInfo chunkedJsonFileInfo = new FileInfo(chunkedJsonFilePath);

                await using var chunkedFileStream = chunkedJsonFileInfo.Create();
                await _streamableTranscriptionExporter.ToJsonAsync(transcriptionResults[i], chunkedFileStream,
                    cancellationToken);
                chunkedJsonFiles.Add(chunkedJsonFileInfo);
            }

            var mergedJsonTranscriptionResult =
                await TranscriptionResultMerger.MergeFilesAsync(chunkedJsonFiles, null, cancellationToken);

            await using var mergedFileStream = File.Create(processedFilePath);
            await _streamableTranscriptionExporter.ToJsonAsync(mergedJsonTranscriptionResult, mergedFileStream,
                cancellationToken);

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation("Transcribed text: {Text}", mergedJsonTranscriptionResult.Text);
            }
        }
        else
        {
            var (fileUri, _) =
                await _audioJobStorageService.CreateDownloadUrlAsync(fileKey, DateTime.UtcNow.AddHours(1),
                    cancellationToken);

            using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var transcriptionTask =
                _groqTranscriptionService.TranscribeAsync(new TranscriptionSource.Url(fileUri),
                    TranscriptionModel.WhisperV3LargeTurbo,
                    progressCts.Token);
            var progressTask = PublishProgressAsync(
                transcriptionTask,
                fileKey,
                progressCts.Token);

            TranscriptionResult transcriptionResult;
            try
            {
                transcriptionResult = await transcriptionTask;
            }
            finally
            {
                await progressCts.CancelAsync();
                await progressTask;
            }

            await using var fileStream = File.Create(processedFilePath);
            await _streamableTranscriptionExporter.ToJsonAsync(transcriptionResult, fileStream, cancellationToken);

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation("Transcribed text: {Text}", transcriptionResult.Text);
            }
        }
    }

    private async Task TranscribeWithCloudflare(string toProcessFilePath, string outputFilePath,
        string fileKey, CancellationToken cancellationToken)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("Transcribing with CLoudflare");
        }

        using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var transcriptionTask =
            _cloudFlareTranscriptionService.TranscribeAsync(
                new TranscriptionSource.FilePath(new FileInfo(toProcessFilePath)),
                TranscriptionModel.WhisperV3LargeTurbo,
                cancellationToken);
        var progressTask = PublishProgressAsync(
            transcriptionTask,
            fileKey,
            progressCts.Token);

        TranscriptionResult transcriptionResult;
        try
        {
            transcriptionResult = await transcriptionTask;
        }
        finally
        {
            await progressCts.CancelAsync();
            await progressTask;
        }

        await using var fileStream = File.Create(outputFilePath);
        await _streamableTranscriptionExporter.ToJsonAsync(transcriptionResult, fileStream, cancellationToken);

        if (_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("Transcribed text: {Text}", transcriptionResult.Text);
        }
    }

    private async Task<bool> IsFileValid(string unProcessedObjectKey, CancellationToken cancellationToken)
    {
        var isValidFile =
            await _audioJobStorageService.IsWhisperCompatibleWavAsync(unProcessedObjectKey, null,
                cancellationToken: cancellationToken);

        if (!isValidFile)
        {
            _logger.LogWarning("File with key: {FileKey} is invalid", unProcessedObjectKey);
            return false;
        }

        return true;
    }

    // *NOTE, using fileKey here to uniquely identify this job
    private static (string toProcessDir, string processedDir) CreateWorkingDirs(string fileKey)
    {
        string toProcessDir = Path.Combine(AppContext.BaseDirectory, "MediaFiles", "ToProcess", fileKey);
        string processedDir = Path.Combine(AppContext.BaseDirectory, "MediaFiles", "Processed", fileKey);
        Directory.CreateDirectory(toProcessDir);
        Directory.CreateDirectory(processedDir);

        return (toProcessDir, processedDir);
    }

    private static (string toProcessFilePath, string processedFilePath) CreateWorkingFilePaths(string toProcessDir,
        string processedDir, string fileKey)
    {
        string toProcessFilePath = Path.Combine(toProcessDir, Path.GetFileName(fileKey));
        string processedFileName = Path.ChangeExtension(Path.GetFileName(fileKey), ".json");
        string processedFilePath = Path.Combine(processedDir, processedFileName);

        return (toProcessFilePath, processedFilePath);
    }

    private async Task<FileInfo> DownloadAndSaveFileAsync(string toProcessFilePath, string unProcessedObjectKey,
        CancellationToken cancellationToken)
    {
        await using var s3Stream =
            await _audioJobStorageService.DownloadAudioAsync(unProcessedObjectKey, cancellationToken);
        await using var fileStream = File.Create(toProcessFilePath);
        await s3Stream.CopyToAsync(fileStream, cancellationToken);
        return new FileInfo(toProcessFilePath);
    }

    private async Task PublishProgressAsync(
        Task transcriptionTask,
        string unprocessedWavFileObjectKey,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (transcriptionTask.IsCompleted)
                    break;

                await _messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
                    new TranscriptionProcessMessage(JobStatus.Processing, unprocessedWavFileObjectKey, null));
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
