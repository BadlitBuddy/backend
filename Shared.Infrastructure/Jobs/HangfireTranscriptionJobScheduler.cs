using Hangfire;
using Shared.Abstractions.Jobs;

namespace Shared.Infrastructure.Jobs;

public class HangfireTranscriptionJobScheduler : ITranscriptionJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireTranscriptionJobScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public string EnqueueTranscriptionJob(string objectFileKey, CancellationToken cancellationToken)
    {
        return _backgroundJobClient.Enqueue<ITranscriptionJob>(jobService =>
            jobService.TranscribeFileAsync(objectFileKey, cancellationToken)
        );
    }
}