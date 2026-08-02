using NanoidDotNet;
using Shared.Abstractions.Storage;

namespace Shared.Infrastructure.Storage;

public class StoragePathBuilder : IStoragePathBuilder
{
    private const int DefaultIdSize = 10;

    /// <summary>
    /// Generates a unique key for unprocessed uploads.
    /// Example: {userId}/unprocessed/{shortId}-{fileName}
    /// </summary>
    public async Task<string> ForUnprocessedFileAsync(string userId, string originalFileName)
    {
        var shortId = await Nanoid.GenerateAsync(size: DefaultIdSize);
        var safeFileName = Path.GetFileName(originalFileName);

        return $"{userId}/unprocessed/{shortId}-{safeFileName}";
    }

    public (string userId, string originalName) ExtractUnprocessedFileKeyParts(string objectKey)
    {
        var parts = objectKey.Split('/');
        var userId = parts[0];
        string originalFileName = parts[^1].Substring(11);

        return (userId, originalFileName);
    }

    /// <summary>
    /// Generates a key for processed uploads.
    /// Example: {userId}/processed/{shortId}-{fileName}
    /// </summary>
    public async Task<string> ForProcessedFileAsync(string userId, string originalFileName)
    {
        var shortId = await Nanoid.GenerateAsync(size: DefaultIdSize);
        var safeFileName = Path.GetFileName(originalFileName);

        return $"{userId}/processed/{shortId}-{safeFileName}";
    }

    public (string userId, string originalName) ExtractProcessedFileKeyParts(string objectKey)
    {
        var parts = objectKey.Split('/');
        var userId = parts[0];
        string originalFileName = parts[^1].Substring(11);

        return (userId, originalFileName);
    }
}
