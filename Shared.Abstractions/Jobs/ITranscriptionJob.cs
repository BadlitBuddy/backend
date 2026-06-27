namespace Shared.Abstractions.Jobs;

public interface ITranscriptionJob
{
    public Task TranscribeFileAsync(string fileKey, CancellationToken cancellationToken);
}