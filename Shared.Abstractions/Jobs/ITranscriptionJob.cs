namespace Shared.Abstractions.Jobs;

public interface ITranscriptionJob
{
    public Task TranscribeFileAsync(string bucketName, string fileKey, CancellationToken cancellationToken);
}