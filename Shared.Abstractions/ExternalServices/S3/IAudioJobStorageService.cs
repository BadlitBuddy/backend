namespace Shared.Abstractions.ExternalServices.S3;

public interface IAudioJobStorageService
{
    Task<bool> IsStorageAvailableAsync(CancellationToken cancellationToken);
    Task<IEnumerable<AudioJobDto>> GetPendingJobsAsync(int batchSize, CancellationToken cancellationToken);
    Task<Stream> DownloadAudioAsync(string fileKey, CancellationToken cancellationToken);
    Task<bool> DeleteAudioAsync(string fileKey, CancellationToken cancellationToken);
}
