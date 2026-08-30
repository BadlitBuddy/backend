namespace Shared.Abstractions.Jobs;

public interface ITranscriptionJob
{
    public Task TranscribeFileAsync(int transcriptId, Guid userId, int organizationId, CancellationToken cancellationToken);
    public Task TranscribeFileWithWhisperTinyEnAsync(string fileKey, CancellationToken cancellationToken);
}
