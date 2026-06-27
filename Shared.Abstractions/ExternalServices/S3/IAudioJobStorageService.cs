namespace Shared.Abstractions.ExternalServices.S3;

public interface IAudioJobStorageService
{
    Task<bool> IsStorageAvailableAsync(CancellationToken cancellationToken);

    Task<bool> IsWhisperCompatibleWavAsync(string fileKey, long? maxSizeBytes = 100L * 1024 * 1024,
        CancellationToken cancellationToken = default);

    Task<UploadUrlDto> CreateUploadUrlAsync(string userId, string originalFileName);
    Task<IEnumerable<AudioJobDto>> GetPendingJobsAsync(int batchSize, CancellationToken cancellationToken);
    Task<Stream> DownloadAudioAsync(string fileKey, CancellationToken cancellationToken);
    Task<bool> DeleteAudioAsync(string fileKey, CancellationToken cancellationToken);
}