using Microsoft.Extensions.Logging;
using Shared.Abstractions.ExternalServices.S3;
using Shared.Abstractions.Jobs;
using Shared.Abstractions.Services;

namespace Shared.Infrastructure.Jobs;

public class TranscriptionJob : ITranscriptionJob
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly ILogger<TranscriptionJob> _logger;

    public TranscriptionJob(
        ITranscriptionService transcriptionService, IAudioJobStorageService audioJobStorageService,
        ILogger<TranscriptionJob> logger
    )
    {
        _transcriptionService = transcriptionService;
        _audioJobStorageService = audioJobStorageService;
        _logger = logger;
    }
    
    public async Task TranscribeFileAsync(string bucketName, string fileKey, CancellationToken cancellationToken)
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "MediaFiles","ToProcess", Path.GetFileName(fileKey));
        
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
            _logger.LogInformation("Starting Whisper transcription for: {FileKey}",fileKey);

            await using (var s3Stream = await _audioJobStorageService.DownloadAudioAsync(fileKey, cancellationToken))
            await using (var fileStream = File.Create(filePath))
            {
                await s3Stream.CopyToAsync(fileStream, cancellationToken);
            }

            await using (var stream = File.OpenRead(filePath))
            {
                // await _transcriptionService.TranscribeAsync(stream, cancellationToken);
                await foreach (var segment in _transcriptionService.TranscribeAsync(stream, cancellationToken))
                {
                    Console.WriteLine($"{segment.Start}->{segment.End}: {segment.Text}");
                    // using var writer = new StreamWriter(outputPath, append: false);
        
                    // Write to the file immediately without buffering all segments in RAM
                    // await writer.WriteLineAsync(line);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {FileKey}",fileKey);
        }
        finally
        {
            await _audioJobStorageService.DeleteAudioAsync(fileKey, cancellationToken);
            if (File.Exists(filePath)) File.Delete(filePath);
        
            _logger.LogInformation("Finished transcription and cleanup for: {FileKey}",fileKey);
        }
    }
}