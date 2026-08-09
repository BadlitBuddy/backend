namespace Shared.Abstractions.Services;

public abstract record TranscriptionSource
{
    public sealed record FilePath(FileInfo FileInfo) : TranscriptionSource;

    public sealed record Url(Uri Uri) : TranscriptionSource;

    public sealed record Stream(System.IO.Stream StreamContent) : TranscriptionSource;
}

public enum TranscriptionModel
{
    WhisperV3LargeTurbo = 0,
    WhisperTinyEn = 1
}

public interface IStreamingTranscriptionService
{
    IAsyncEnumerable<TranscriptionSegment> TranscribeStreamingAsync(Stream fileStream,
        CancellationToken cancellationToken);

    // TODO: remove unnecessary methods
    Task TranscribeAndWriteAsSrtFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken);

    Task TranscribeAndWriteAsTxtFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken);

    Task TranscribeAndWriteAsVttFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken);
}

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(
        TranscriptionSource source,
        TranscriptionModel transcriptionModel,
        CancellationToken cancellationToken = default);
}
