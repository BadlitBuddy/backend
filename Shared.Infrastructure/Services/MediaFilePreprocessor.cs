using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using FFMpegCore;
using FFMpegCore.Pipes;
using NanoidDotNet;
using Shared.Abstractions.Services;

namespace Shared.Infrastructure.Services;

public readonly record struct Base64Chunk(
    byte[] Buffer,
    int Length)
{
    public ReadOnlyMemory<byte> Memory => Buffer.AsMemory(0, Length);

    public void Return()
    {
        ArrayPool<byte>.Shared.Return(Buffer);
    }
}

public static class MediaFilePreprocessor
{
    public static async IAsyncEnumerable<Base64Chunk> EncodeToUtf8Async(
        Stream input,
        int bufferSize = 30_720,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        byte[] inputBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            int leftover = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int read = await input.ReadAsync(
                    inputBuffer.AsMemory(leftover, bufferSize - leftover),
                    cancellationToken);

                if (read == 0)
                    break;

                int total = leftover + read;

                int bytesToEncode = (total / 3) * 3;
                leftover = total - bytesToEncode;

                if (bytesToEncode > 0)
                {
                    byte[] output =
                        ArrayPool<byte>.Shared.Rent(
                            Base64.GetMaxEncodedToUtf8Length(bytesToEncode));

                    Base64.EncodeToUtf8(
                        inputBuffer.AsSpan(0, bytesToEncode),
                        output,
                        out _,
                        out int written,
                        isFinalBlock: false);

                    yield return new Base64Chunk(output, written);
                }

                if (leftover > 0)
                {
                    inputBuffer.AsSpan(bytesToEncode, leftover).CopyTo(inputBuffer.AsSpan(0, leftover));
                }
            }

            if (leftover > 0)
            {
                byte[] output =
                    ArrayPool<byte>.Shared.Rent(
                        Base64.GetMaxEncodedToUtf8Length(leftover));

                Base64.EncodeToUtf8(
                    inputBuffer.AsSpan(0, leftover),
                    output,
                    out _,
                    out int written,
                    isFinalBlock: true);

                yield return new Base64Chunk(output, written);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(inputBuffer);
        }
    }

    public static async IAsyncEnumerable<string> EncodeToBase64Async(
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

    public static async Task<string> ExtractAudioChunkFromWavAndWriteToTmpAsync(
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
