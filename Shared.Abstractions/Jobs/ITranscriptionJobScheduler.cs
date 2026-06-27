namespace Shared.Abstractions.Jobs;

public interface ITranscriptionJobScheduler
{
    string EnqueueTranscriptionJob(string objectFileKey, CancellationToken cancellationToken);
}