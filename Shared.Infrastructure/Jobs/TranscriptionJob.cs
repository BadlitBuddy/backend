using Api.Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.ExternalServices.S3;
using Shared.Abstractions.Jobs;
using Shared.Abstractions.Repositories;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;

namespace Shared.Infrastructure.Jobs;

public class TranscriptionJob : ITranscriptionJob
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly ILogger<TranscriptionJob> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ITranscriptionJobRepository _transcriptionJobRepository;

    public TranscriptionJob(
        ITranscriptionService transcriptionService, IAudioJobStorageService audioJobStorageService,
        ILogger<TranscriptionJob> logger, IHostEnvironment hostEnvironment, IMessagePublisher messagePublisher, 
        ITranscriptionJobRepository  transcriptionJobRepository
    )
    {
        _transcriptionService = transcriptionService;
        _audioJobStorageService = audioJobStorageService;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _messagePublisher = messagePublisher;
        _transcriptionJobRepository = transcriptionJobRepository;
    }

    public async Task TranscribeFileAsync(string fileKey, CancellationToken cancellationToken)
    {
        var isValidFile =
            await _audioJobStorageService.IsWhisperCompatibleWavAsync(fileKey, cancellationToken: cancellationToken);
        if (!isValidFile)
        {
            _logger.LogWarning("File with key: {FileKey} is invalid", fileKey);
            return;
        }

        string toProcessDir = Path.Combine(AppContext.BaseDirectory, "MediaFiles", "ToProcess");
        string processedDir = Path.Combine(AppContext.BaseDirectory, "MediaFiles", "Processed");
        Directory.CreateDirectory(toProcessDir);
        Directory.CreateDirectory(processedDir);

        string toProcessFilePath = Path.Combine(toProcessDir, Path.GetFileName(fileKey));

        string outputFileName = Path.ChangeExtension(Path.GetFileName(fileKey), ".txt");
        string outputFilePath = Path.Combine(processedDir, outputFileName);

        var parts = fileKey.Split('/');
        var userId = parts[0];
        var originalFileName = Path.ChangeExtension(parts[^1].Substring(11), ".txt");

        var outputObjectKey = "";

        try
        {
            _logger.LogInformation("Starting Whisper transcription for: {FileKey}", fileKey);

            await using (var s3Stream = await _audioJobStorageService.DownloadAudioAsync(fileKey, cancellationToken))
            await using (var fileStream = File.Create(toProcessFilePath))
            {
                await s3Stream.CopyToAsync(fileStream, cancellationToken);
            }
            
            await _transcriptionJobRepository.UpdateStatusAsync(fileKey, outputObjectKey, TranscriptionJobStatus.Processing, new Guid(userId));
            
            await using (var stream = File.OpenRead(toProcessFilePath))
            await using (var writer = new StreamWriter(outputFilePath, append: false))
            {
                await foreach (var segment in _transcriptionService.TranscribeAsync(stream, cancellationToken))
                {
                    var line = $"{segment.Start}->{segment.End}: {segment.Text}";
                    if (_hostEnvironment.IsDevelopment())
                    {
                        _logger.LogInformation(line);
                    }

                    await writer.WriteLineAsync(line);
                }
            }

            await using (var uploadStream = new FileStream(
                             outputFilePath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.Read,
                             bufferSize: 4096,
                             useAsync: true))
            {
                var uploadResult = await _audioJobStorageService.UploadTranscriptionAsync(
                    userId,
                    originalFileName,
                    uploadStream,
                    cancellationToken);

                outputObjectKey = uploadResult;
            }

            await _transcriptionJobRepository.UpdateStatusAsync(fileKey, outputObjectKey, TranscriptionJobStatus.Completed, new Guid(userId));
            await _audioJobStorageService.DeleteAudioAsync(fileKey, cancellationToken);

            _logger.LogInformation("Finished transcription and cleanup for: {FileKey}", fileKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {FileKey}", fileKey);
        }
        finally
        {
            if (File.Exists(toProcessFilePath)) File.Delete(toProcessFilePath);
            if (File.Exists(outputFilePath)) File.Delete(outputFilePath);

            await _messagePublisher.PublishAsync(MessageChannel.TranscriptionFinished,
                new TranscriptionFinishedMessage(userId, fileKey, outputObjectKey));
        }
    }
}
