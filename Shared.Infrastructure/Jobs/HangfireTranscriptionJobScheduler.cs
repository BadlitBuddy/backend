using Hangfire;
using Microsoft.Extensions.Options;
using Shared.Abstractions.Jobs;

namespace Shared.Infrastructure.Jobs;

public class HangfireTranscriptionJobScheduler : ITranscriptionJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IOptions<S3Options> _s3Options;

    public HangfireTranscriptionJobScheduler(IBackgroundJobClient backgroundJobClient, IOptions<S3Options> s3Options)
    {
        _backgroundJobClient = backgroundJobClient;
        _s3Options = s3Options;
    }

    public string EnqueueTranscriptionJob(string objectFileKey, CancellationToken cancellationToken)
    {
        return _backgroundJobClient.Enqueue<ITranscriptionJob>(jobService =>
            jobService.TranscribeFileAsync(_s3Options.Value.BucketName!, objectFileKey, cancellationToken)
        );
    }
}