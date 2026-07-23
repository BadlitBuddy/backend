namespace Shared.Abstractions.Services;

public interface IMediaFilePreprocessor
{
    IAsyncEnumerable<string> EncodeToBase64Async(Stream inputStream, CancellationToken cancellationToken = default);
}
