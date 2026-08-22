using FFMpegCore;

namespace Shared.Infrastructure.Services;

public static class AudioFileProcessor
{
    // TODO: Add concurrency
    public static async Task<FileInfo[]> ChunkFileAsync(
        FileInfo inputFile,
        long maxChunkSizeBytes,
        string outputDirectory,
        double offsetMinutes = 0,
        CancellationToken cancellationToken = default)
    {
        if (inputFile is null || !inputFile.Exists)
        {
            throw new FileNotFoundException("Input file not found.", inputFile?.FullName);
        }

        if (maxChunkSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChunkSizeBytes), "Must be greater than zero.");
        }

        if (offsetMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offsetMinutes), "Must be zero or greater.");
        }

        IMediaAnalysis mediaInfo = await FFProbe.AnalyseAsync(inputFile.FullName, cancellationToken: cancellationToken);
        TimeSpan totalDuration = mediaInfo.Duration;

        if (totalDuration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Could not determine a valid duration for the source file.");
        }

        long totalBytes = inputFile.Length;
        double bytesPerSecond = totalBytes / totalDuration.TotalSeconds;
        TimeSpan offset = TimeSpan.FromMinutes(offsetMinutes);

        double overlapBytes = offset.TotalSeconds * bytesPerSecond;
        long effectiveMaxChunkBytes = maxChunkSizeBytes - (long)Math.Ceiling(overlapBytes);
        if (effectiveMaxChunkBytes <= 0)
        {
            throw new ArgumentException(
                "The overlap offset duration produces a size larger than maxChunkSizeBytes itself.",
                nameof(offsetMinutes));
        }

        int chunkCount = (int)Math.Ceiling(totalBytes / (double)effectiveMaxChunkBytes);
        chunkCount = Math.Max(chunkCount, 1);

        Directory.CreateDirectory(outputDirectory);
        if (chunkCount == 1)
        {
            string singleOutputPath = Path.Combine(
                outputDirectory,
                $"{Path.GetFileNameWithoutExtension(inputFile.Name)}_chunk0{inputFile.Extension}");

            File.Copy(inputFile.FullName, singleOutputPath, overwrite: true);
            return [new FileInfo(singleOutputPath)];
        }


        var results = new List<FileInfo>(chunkCount);
        double chunkDurationSeconds = totalDuration.TotalSeconds / chunkCount;

        for (int i = 0; i < chunkCount; i++)
        {
            TimeSpan start = TimeSpan.FromSeconds(chunkDurationSeconds * i);

            TimeSpan duration;
            if (i == chunkCount - 1)
            {
                duration = totalDuration - start;
            }
            else
            {
                duration = TimeSpan.FromSeconds(chunkDurationSeconds) + offset;

                TimeSpan maxAvailable = totalDuration - start;
                if (duration > maxAvailable)
                    duration = maxAvailable;
            }

            if (duration <= TimeSpan.Zero)
                continue;

            string outputPath = Path.Combine(
                outputDirectory,
                $"{Path.GetFileNameWithoutExtension(inputFile.Name)}_chunk{i}{inputFile.Extension}");

            await FFMpegArguments
                .FromFileInput(inputFile.FullName, verifyExists: true, options => options
                    .Seek(start))
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithDuration(duration)
                    .WithCustomArgument("-c:a copy")
                    .WithCustomArgument("-avoid_negative_ts make_zero"))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            results.Add(new FileInfo(outputPath));
        }

        return results.ToArray();
    }

    /// <summary>
    /// Converts a WAV file to FLAC format.
    /// </summary>
    /// <param name="inputFile">The source WAV file.</param>
    /// <param name="outputDirectory">Directory where the FLAC file will be written.</param>
    /// <param name="compressionLevel">
    /// FLAC compression level (0 = fastest/largest, 8 = slowest/smallest). Defaults to 5.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A FileInfo for the resulting FLAC file.</returns>
    public static async Task<FileInfo> ConvertWavToFlacAsync(
        FileInfo inputFile,
        string outputDirectory,
        int compressionLevel = 5,
        CancellationToken cancellationToken = default)
    {
        if (!inputFile.Exists)
            throw new FileNotFoundException("Input WAV file not found.", inputFile.FullName);
        if (compressionLevel < 0 || compressionLevel > 8)
            throw new ArgumentOutOfRangeException(nameof(compressionLevel), "Must be between 0 and 8.");

        Directory.CreateDirectory(outputDirectory);

        string outputPath = Path.Combine(
            outputDirectory,
            $"{Path.GetFileNameWithoutExtension(inputFile.Name)}.flac");

        await FFMpegArguments
            .FromFileInput(inputFile.FullName, verifyExists: true)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithAudioCodec("flac")
                .WithCustomArgument($"-compression_level {compressionLevel}"))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return new FileInfo(outputPath);
    }
}
