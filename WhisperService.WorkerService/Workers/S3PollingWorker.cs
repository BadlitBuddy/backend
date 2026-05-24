using WhisperService.Core.Services;
using WhisperService.WorkerService.Channels;
using WhisperService.WorkerService.Contracts;

namespace WhisperService.WorkerService.Workers;

public class S3PollingWorker : BackgroundService
{
    private readonly ILogger<S3PollingWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly TranscriptionQueueChannel _channel;
    private readonly IAudioJobStorageService _audioJobStorageService;

    public S3PollingWorker(
        ILogger<S3PollingWorker> logger, 
        TranscriptionQueueChannel channel, IAudioJobStorageService  audioJobStorageService
        )
    {
        _logger = logger;
        _channel = channel;
        _audioJobStorageService = audioJobStorageService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await _audioJobStorageService.IsStorageAvailableAsync(stoppingToken))
        {
            throw new InvalidOperationException("Storage backend is unreachable.");
        }
        
        using PeriodicTimer timer = new(_pollingInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("Polling tick triggered at: {time}", DateTimeOffset.Now);
                var pendingJobs = await _audioJobStorageService.GetPendingJobsAsync(batchSize: 5, stoppingToken);
                var jobsList = pendingJobs.ToList();

                if (jobsList.Count > 0)
                {
                    foreach (var job in jobsList)
                    {
                        bool wasAdded = await _channel.WriteAsync(new TranscriptionJobContract(AudioJob: job), stoppingToken);
                        if (!wasAdded)
                        {
                            _logger.LogInformation("Skipped adding transcription for: {FileKey}, already in queue.",job.FileKey);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("The bucket is currently empty.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("S3 Polling Service is stopping via cancellation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled error occurred in the s3 Polling Service.");
        }
    }
}
