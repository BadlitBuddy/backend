namespace Shared.Abstractions.Storage;

public interface IStoragePathBuilder
{
    Task<string> ForUnprocessedFileAsync(string userId, string originalFileName);
    (string userId, string originalName) ExtractUnprocessedFileKeyParts(string objectKey);
    Task<string> ForProcessedFileAsync(string userId, string originalFileName);
    (string userId, string originalName) ExtractProcessedFileKeyParts(string objectKey);
}
