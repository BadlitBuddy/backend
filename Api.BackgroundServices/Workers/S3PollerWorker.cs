using Hangfire;
using Shared.Abstractions.ExternalServices.S3;
using Shared.Abstractions.Jobs;

namespace Api.BackgroundServices.Workers;

public class S3PollerWorker : BackgroundService
{
    private HashSet<string> ProcessedKeys = new HashSet<string>();
    private readonly ILogger<S3PollerWorker> _logger;
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public S3PollerWorker(
        ILogger<S3PollerWorker> logger, IAudioJobStorageService audioJobStorageService,
        IBackgroundJobClient backgroundJobClient
    )
    {
        _logger = logger;
        _audioJobStorageService = audioJobStorageService;
        _backgroundJobClient = backgroundJobClient;
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
                var pendingJobs = (await _audioJobStorageService.GetPendingJobsAsync(batchSize: 5, stoppingToken))
                    .ToList();
                bool foundNewJobs = false;

                foreach (var job in pendingJobs)
                {
                    if (ProcessedKeys.Add(job.FileKey))
                    {
                        foundNewJobs = true;
                        _logger.LogInformation("Processing job: {job}", job.FileKey);
                        _backgroundJobClient.Enqueue<ITranscriptionJob>(jobService =>
                            jobService.TranscribeFileAsync(job.FileKey, stoppingToken)
                        );
                    }
                }

                if (!foundNewJobs && !pendingJobs.Any())
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