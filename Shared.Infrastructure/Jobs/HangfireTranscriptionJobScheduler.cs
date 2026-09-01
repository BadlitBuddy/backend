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

    public string EnqueueTranscriptionJob(int transcriptId, Guid userId, int organizationId, CancellationToken cancellationToken)
    {
        return _backgroundJobClient.Enqueue<ITranscriptionJob>(jobService =>
            jobService.TranscribeFileAsync(transcriptId, userId, organizationId, cancellationToken)
        );
    }

    public string EnqueueTranscriptionJobWithWhisperTinyEn(string objectFileKey, CancellationToken cancellationToken)
    {
        return _backgroundJobClient.Enqueue<TranscriptionJob.TranscriptionJob>(jobService =>
            jobService.TranscribeFileWithWhisperTinyEnAsync(objectFileKey, cancellationToken)
        );
    }
}
