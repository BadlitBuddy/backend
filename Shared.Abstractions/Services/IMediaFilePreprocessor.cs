namespace Shared.Abstractions.Services;

public interface IMediaFilePreprocessor
{
    IAsyncEnumerable<string> EncodeToBase64Async(Stream inputStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts a chunk of audio from a WAV file and writes it to a temporary file.
    /// </summary>
    /// <param name="filePath">The path to the source WAV file.</param>
    /// <param name="startTime">The starting timestamp for the audio chunk.</param>
    /// <param name="stopTime">The optional stopping timestamp for the audio chunk.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file path of the temporary output file.</returns>
    Task<string> ExtractAudioChunkFromWavAndWriteToTmpAsync(
        string filePath,
        TimeSpan startTime,
        TimeSpan? stopTime = null,
        CancellationToken cancellationToken = default);
}
