using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;
using Shared.Infrastructure.Services;

namespace Shared.Infrastructure.Jobs.TranscriptionJob.TranscriptionProviders;

public class GroqTranscriber
{
    private const long MaxLocalFileSizeToProcess = 24 * 1024 * 1024;

    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly ILogger<GroqTranscriber> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IStreamableTranscriptionExporter _streamableTranscriptionExporter;
    private readonly ITranscriptionService _groqTranscriptionService;

    public GroqTranscriber(
        ILogger<GroqTranscriber> logger, IHostEnvironment hostEnvironment,
        [FromKeyedServices(TranscriptionProvider.Groq)] ITranscriptionService groqTranscriptionService,
        IMessagePublisher messagePublisher,
        IStreamableTranscriptionExporter streamableTranscriptionExporter,
        IAudioJobStorageService audioJobStorageService)
    {
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _groqTranscriptionService = groqTranscriptionService;
        _messagePublisher = messagePublisher;
        _streamableTranscriptionExporter = streamableTranscriptionExporter;
        _audioJobStorageService = audioJobStorageService;
    }

    public async Task Transcribe(FileInfo toProcessFile, string processedFilePath, string processedDir,
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

            await using var fileStream = File.Create(processedFilePath);
            await _streamableTranscriptionExporter.ToJsonAsync(transcriptionResult, fileStream, cancellationToken);

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation("Transcribed text: {Text}", transcriptionResult.Text);
            }
        }
    }
}
