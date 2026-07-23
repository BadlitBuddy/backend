using System.Buffers;
using System.Runtime.CompilerServices;
using FFMpegCore;
using FFMpegCore.Pipes;
using NanoidDotNet;
using Shared.Abstractions.Services;

namespace Shared.Infrastructure.Services;

public class MediaFilePreprocessor : IMediaFilePreprocessor
{
    public async IAsyncEnumerable<string> EncodeToBase64Async(
        Stream inputeStream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Use a buffer size that is a multiple of 3 (e.g., 30720 = 10240 * 3)
        // to prevent intermediate Base64 padding ('=').
        const int bufferSize = 30720;

        // Rent a buffer to avoid continuous allocations
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int leftoverCount = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int bytesRead = await inputeStream.ReadAsync(
                    buffer.AsMemory(leftoverCount, bufferSize - leftoverCount),
                    cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                int totalBytes = leftoverCount + bytesRead;

                int bytesToEncode = (totalBytes / 3) * 3;
                int newLeftoverCount = totalBytes - bytesToEncode;

                if (bytesToEncode > 0)
                {
                    yield return Convert.ToBase64String(buffer.AsSpan(0, bytesToEncode));
                }

                if (newLeftoverCount > 0)
                {
                    buffer.AsSpan(bytesToEncode, newLeftoverCount).CopyTo(buffer);
                }

                leftoverCount = newLeftoverCount;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (leftoverCount > 0)
            {
                yield return Convert.ToBase64String(buffer.AsSpan(0, leftoverCount));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task<string> ExtractAudioChunkFromWavAndWriteToTmpAsync(
        string filePath,
        TimeSpan startTime,
        TimeSpan? stopTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("WAV file not found.", filePath);

        if (startTime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(startTime));

        if (stopTime.HasValue && stopTime.Value <= startTime)
            throw new ArgumentOutOfRangeException(nameof(stopTime), "stopTime must be greater than startTime.");

        var shortId = await Nanoid.GenerateAsync(size: 8);
        var tempOutputPath = Path.Combine(Path.GetTempPath(), $"{shortId}.wav");

        try
        {
            await FFMpegArguments
                .FromFileInput(filePath, verifyExists: true, options => options
                    .Seek(startTime))
                .OutputToFile(tempOutputPath, overwrite: true, options =>
                {
                    if (stopTime.HasValue)
                    {
                        options.WithDuration(stopTime.Value - startTime);
                    }

                    options
                        .WithAudioCodec("pcm_s16le")
                        .ForceFormat("wav");
                })
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            return tempOutputPath;
        }
        catch
        {
            if (File.Exists(tempOutputPath))
            {
                File.Delete(tempOutputPath);
            }

            throw;
        }
    }
}
