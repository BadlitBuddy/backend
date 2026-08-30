namespace Shared.Abstractions.Jobs;

public interface ITranscriptionJobScheduler
{
    string EnqueueTranscriptionJob(int transcriptId, Guid userId, int organizationId, CancellationToken cancellationToken);
    string EnqueueTranscriptionJobWithWhisperTinyEn(string objectFileKey, CancellationToken cancellationToken);
}
