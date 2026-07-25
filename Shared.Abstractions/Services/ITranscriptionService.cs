namespace Shared.Abstractions.Services;

// TODO: Use ISP here for unnecessary methods
public interface IStreamingTranscriptionService
{
    IAsyncEnumerable<TranscriptionSegment> TranscribeAsync(Stream fileStream, CancellationToken cancellationToken);

    Task TranscribeAndWriteAsSrtFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken);

    Task TranscribeAndWriteAsTxtFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken);

    Task TranscribeAndWriteAsVttFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken);
}
