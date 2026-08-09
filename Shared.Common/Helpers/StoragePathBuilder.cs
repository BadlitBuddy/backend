using NanoidDotNet;

namespace Shared.Common.Helpers;

public static class StoragePathBuilder
{
    private const int DefaultIdSize = 10;

    // todo: need to fix this, userid is nullable
    /// <summary>
    /// Generates a unique key for unprocessed uploads.
    /// Example: {userId}/unprocessed/{shortId}-{fileName}
    /// </summary>
    public static string ForUnprocessedFileAsync(string userId, string originalFileName)
    {
        var shortId = Nanoid.Generate(size: DefaultIdSize);
        var safeFileName = Path.GetFileName(originalFileName);

        return $"{userId}/unprocessed/{shortId}-{safeFileName}";
    }

    public static (string userId, string originalName) ExtractUnprocessedFileKeyParts(string objectKey)
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
    public static string ForProcessedFileAsync(string userId, string originalFileName)
    {
        var shortId = Nanoid.Generate(size: DefaultIdSize);
        var safeFileName = Path.GetFileName(originalFileName);

        return $"{userId}/processed/{shortId}-{safeFileName}";
    }

    public static (string userId, string originalName) ExtractProcessedFileKeyParts(string objectKey)
    {
        var parts = objectKey.Split('/');
        var userId = parts[0];
        string originalFileName = parts[^1].Substring(11);

        return (userId, originalFileName);
    }
}
