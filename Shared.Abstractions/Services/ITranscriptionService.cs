namespace Shared.Abstractions.Services;

public interface ITranscriptionService
{
    IAsyncEnumerable<TranscriptionSegment> TranscribeAsync(Stream fileStream, CancellationToken cancellationToken);

    Task TranscribeAndWriteAsSrtFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken);

    Task TranscribeAndWriteAsTxtFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken);

    Task TranscribeAndWriteAsVttFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken);
}
