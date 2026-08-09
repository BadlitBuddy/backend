namespace Shared.Abstractions.Jobs;

public interface ITranscriptionJobScheduler
{
    string EnqueueTranscriptionJob(string objectFileKey, CancellationToken cancellationToken);
    string EnqueueTranscriptionJobWithWhisperTinyEn(string objectFileKey, CancellationToken cancellationToken);
}
