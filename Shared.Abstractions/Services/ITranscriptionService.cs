namespace Shared.Abstractions.Services;

public interface IStreamingTranscriptionService
{
    IAsyncEnumerable<TranscriptionSegment> TranscribeStreamingAsync(Stream fileStream,
        CancellationToken cancellationToken);

    // TODO: Use ISP here for unnecessary methods
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
        string filePath,
        CancellationToken cancellationToken = default);
}
