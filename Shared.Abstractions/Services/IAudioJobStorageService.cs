namespace Shared.Abstractions.Services;

public interface IAudioJobStorageService
{
    Task<bool> IsStorageAvailableAsync(CancellationToken cancellationToken);

    Task<bool> DoesObjectExistAsync(string fileKey, CancellationToken cancellationToken = default);

    Task<bool> IsWhisperCompatibleWavAsync(string fileKey, long? maxSizeBytes = 100L * 1024 * 1024,
        CancellationToken cancellationToken = default);

    Task<TimeSpan?> GetWavDurationAsync(
        string fileKey,
        CancellationToken cancellationToken = default);

    TimeSpan? GetWavDuration(
        FileStream streamInput);

    Task<UploadUrlDto> CreateUploadUrlAsync(string userId, string originalFileName);
    Task<IEnumerable<AudioJobDto>> GetPendingJobsAsync(int batchSize, CancellationToken cancellationToken);

    Task<string> UploadTranscriptionAsync(string userId, string originalFileName, Stream audioStream,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAudioAsync(string fileKey, CancellationToken cancellationToken);
    Task<bool> DeleteAudioAsync(string fileKey, CancellationToken cancellationToken);

    Task<(Uri uri, DateTime expiry)> CreateDownloadUrlAsync(string fileKey,
        CancellationToken cancellationToken = default);
}
