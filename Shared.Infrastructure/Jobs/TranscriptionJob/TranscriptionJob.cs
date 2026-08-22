using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Abstractions.Jobs;
using Shared.Abstractions.Repositories;
using Shared.Abstractions.Services;
using Shared.Common.Helpers;
using Shared.Contracts.Enums;
using Shared.Infrastructure.Constants;
using Shared.Infrastructure.Jobs.TranscriptionJob.TranscriptionProviders;
using Shared.Infrastructure.Services;
using TranscriptionJobStatus = Api.Domain.Enums.TranscriptionJobStatus;

namespace Shared.Infrastructure.Jobs.TranscriptionJob;

public class TranscriptionJob : ITranscriptionJob
{
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly ILogger<TranscriptionJob> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ITranscriptRepository _transcriptRepository;
    private readonly IStreamableTranscriptionExporter _streamableTranscriptionExporter;
    private readonly ITranscriptionService _cloudFlareTranscriptionService;
    private readonly WorkerOptions _workerOptions;
    private readonly CloudFlareTranscriber _cloudFlareTranscriber;
    private readonly GroqTranscriber _groqTranscriber;
    private readonly WhisperNetTranscriber _whisperNetTranscriber;

    public TranscriptionJob(
        IAudioJobStorageService audioJobStorageService,
        ILogger<TranscriptionJob> logger, IHostEnvironment hostEnvironment, IMessagePublisher messagePublisher,
        ITranscriptRepository transcriptRepository, IOptions<WorkerOptions> workerOptions,
        IStreamableTranscriptionExporter streamableTranscriptionExporter,
        [FromKeyedServices(TranscriptionProvider.Cloudflare)]
        ITranscriptionService cloudFlareTranscriptionService,
        CloudFlareTranscriber cloudFlareTranscriber, GroqTranscriber groqTranscriber,
        WhisperNetTranscriber whisperNetTranscriber
    )
    {
        _audioJobStorageService = audioJobStorageService;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _messagePublisher = messagePublisher;
        _transcriptRepository = transcriptRepository;
        _streamableTranscriptionExporter = streamableTranscriptionExporter;
        _cloudFlareTranscriptionService = cloudFlareTranscriptionService;
        _cloudFlareTranscriber = cloudFlareTranscriber;
        _groqTranscriber = groqTranscriber;
        _whisperNetTranscriber = whisperNetTranscriber;
        _workerOptions = workerOptions.Value;
    }

    public async Task TranscribeFileAsync(string fileKey, CancellationToken cancellationToken)
    {
        await _messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
            new TranscriptionProcessMessage(JobStatus.Processing, fileKey, null));

        var isFileValid = await IsFileValid(fileKey, cancellationToken);
        if (!isFileValid)
        {
            throw new InvalidOperationException();
        }

        var (toProcessDir, processedDir) = TranscriptionJobHelpers.CreateWorkingDirs(fileKey);
        var (toProcessFilePath, processedFilePath) =
            TranscriptionJobHelpers.CreateWorkingFilePaths(toProcessDir, processedDir, fileKey);
        var (userId, originalFileName) = StoragePathBuilder.ExtractUnprocessedFileKeyParts(fileKey);

        string? outputObjectKey = null;
        try
        {
            _logger.LogInformation("Starting Whisper transcription for: {FileKey}", fileKey);

            var toProcessFileInfo = await DownloadAndSaveFileAsync(toProcessFilePath, fileKey, cancellationToken);

            switch (_workerOptions.TranscriptionProvider)
            {
                case TranscriptionProvider.WhisperNet:
                    await _whisperNetTranscriber.Transcribe(toProcessFilePath, processedFilePath, fileKey,
                        cancellationToken);
                    break;
                case TranscriptionProvider.Groq:
                    await _groqTranscriber.Transcribe(toProcessFileInfo, processedFilePath, processedDir,
                        fileKey, cancellationToken);
                    break;
                case TranscriptionProvider.Cloudflare:
                    await _cloudFlareTranscriber.Transcribe(toProcessFilePath, processedFilePath, fileKey,
                        cancellationToken);
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
            TranscriptionJobHelpers.CleanDirectory(directory);

            await _messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
                new TranscriptionProcessMessage(JobStatus.Finished, fileKey, outputObjectKey));
        }
    }

    [Queue(HangfireQueueConstants.WhisperTinyEn)]
    public async Task TranscribeFileWithWhisperTinyEnAsync(string fileKey, CancellationToken cancellationToken)
    {
        await _messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
            new TranscriptionProcessMessage(JobStatus.Processing, fileKey, null));

        var isFileValid = await IsFileValid(fileKey, cancellationToken);
        if (!isFileValid)
        {
            throw new InvalidOperationException();
        }

        var (toProcessDir, processedDir) = TranscriptionJobHelpers.CreateWorkingDirs(fileKey);
        var (toProcessFilePath, processedFilePath) =
            TranscriptionJobHelpers.CreateWorkingFilePaths(toProcessDir, processedDir, fileKey);

        var (userId, originalFileName) = StoragePathBuilder.ExtractUnprocessedFileKeyParts(fileKey);

        string? outputObjectKey = null;
        try
        {
            _logger.LogInformation("Starting Whisper transcription for: {FileKey}", fileKey);

            var toProcessFileInfo = await DownloadAndSaveFileAsync(toProcessFilePath, fileKey, cancellationToken);
            var toProcessFileFlac =
                await AudioFileProcessor.ConvertWavToFlacAsync(toProcessFileInfo, processedDir, 5, cancellationToken);

            await _transcriptRepository.UpdateStatusAsync(fileKey, null,
                TranscriptionJobStatus.Processing, new Guid(userId));

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation("Transcribing with CLoudflare Whisper Tiny-en with key: {FileKey}", fileKey);
            }

            const long maxLocalFileSizeToProcess = 24 * 1024 * 1024;
            var isLargeFile = toProcessFileFlac.Length > maxLocalFileSizeToProcess;
            if (isLargeFile)
            {
                const long maxBytesPerChunk = 10 * 1024 * 1024;
                var chunkedFiles = await AudioFileProcessor.ChunkFileAsync(toProcessFileFlac,
                    maxBytesPerChunk,
                    processedDir, 3, cancellationToken);

                if (_hostEnvironment.IsDevelopment())
                {
                    _logger.LogInformation($"Max Bytes per chunk {maxBytesPerChunk} bytes");
                    Array.ForEach(chunkedFiles,
                        file => _logger.LogInformation("Chunked File {FileName} has size {FileLength} bytes", file.Name,
                            file.Length));
                }

                using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                TimeSpan delayBetweenStarts = TimeSpan.FromMilliseconds(3500);
                List<Task<TranscriptionResult>> transcriptionTasks = [];

                foreach (var chunkedFile in chunkedFiles)
                {
                    var file = chunkedFile;

                    transcriptionTasks.Add(Task.Run(async () =>
                    {
                        return await _cloudFlareTranscriptionService.TranscribeAsync(
                            new TranscriptionSource.FilePath(file),
                            TranscriptionModel.WhisperTinyEn,
                            cancellationToken);
                    }, cancellationToken));

                    await Task.Delay(delayBetweenStarts, cancellationToken);
                }

                var tasks = Task.WhenAll(transcriptionTasks);
                var progressTask = TranscriptionJobHelpers.PublishProgressAsync(
                    tasks,
                    fileKey,
                    _messagePublisher,
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

                {
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
            else
            {
                using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var transcriptionTask =
                    _cloudFlareTranscriptionService.TranscribeAsync(
                        new TranscriptionSource.FilePath(toProcessFileInfo),
                        TranscriptionModel.WhisperTinyEn,
                        cancellationToken);
                var progressTask = TranscriptionJobHelpers.PublishProgressAsync(
                    transcriptionTask,
                    fileKey,
                    _messagePublisher,
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {FileKey}", fileKey);
            throw;
        }
        finally
        {
            DirectoryInfo directory = new DirectoryInfo(processedDir);
            TranscriptionJobHelpers.CleanDirectory(directory);

            await _messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
                new TranscriptionProcessMessage(JobStatus.Finished, fileKey, outputObjectKey));
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

    private async Task<FileInfo> DownloadAndSaveFileAsync(string toProcessFilePath, string unProcessedObjectKey,
        CancellationToken cancellationToken)
    {
        await using var s3Stream =
            await _audioJobStorageService.DownloadAudioAsync(unProcessedObjectKey, cancellationToken);
        await using var fileStream = File.Create(toProcessFilePath);
        await s3Stream.CopyToAsync(fileStream, cancellationToken);
        return new FileInfo(toProcessFilePath);
    }
}
