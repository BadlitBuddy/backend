using Shared.Abstractions.ExternalServices.S3;
using WhisperService.WorkerService.Channels;
using WhisperService.Core.Services;

namespace WhisperService.WorkerService.Workers;

public class TranscriptionWorker : BackgroundService
{
    private readonly ILogger<TranscriptionWorker> _logger;
    private readonly ITranscriptionService _transcriptionService;
    private readonly TranscriptionQueueChannel _channel;
    private readonly IAudioJobStorageService _audioJobStorageService;

    public TranscriptionWorker(
        ILogger<TranscriptionWorker> logger, ITranscriptionService transcriptionService, 
        TranscriptionQueueChannel channel, IAudioJobStorageService audioJobStorageService
        )
    {
        _logger = logger;
        _transcriptionService = transcriptionService;
        _channel = channel;
        _audioJobStorageService = audioJobStorageService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Transcription Worker running at: {time}", DateTimeOffset.Now);
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await foreach (var transcriptionJob in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                string fileKey = transcriptionJob.AudioJob.FileKey;
                string filePath = Path.Combine(AppContext.BaseDirectory, "MediaFiles", Path.GetFileName(fileKey));

                try
                {
                    _logger.LogInformation("Starting Whisper transcription for: {FileKey}",fileKey);

                    await using (var s3Stream = await _audioJobStorageService.DownloadAudioAsync(fileKey, stoppingToken))
                    await using (var fileStream = File.Create(filePath))
                    {
                        await s3Stream.CopyToAsync(fileStream, stoppingToken);
                    }

                    await using (var stream = File.OpenRead(filePath))
                    {
                        await _transcriptionService.TranscribeAsync(stream);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process {FileKey}",fileKey);
                }
                finally
                {
                    await _audioJobStorageService.DeleteAudioAsync(fileKey, stoppingToken);
                    _channel.CompleteProcessing(fileKey);
                    if (File.Exists(filePath)) File.Delete(filePath);
        
                    _logger.LogInformation("Finished transcription and cleanup for: {FileKey}",fileKey);
                }
            }

            await Task.Delay(2500, stoppingToken);
        }
    }
}
