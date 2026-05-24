using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using WhisperService.Core.Services;
using WhisperService.WorkerService.AppSettings;
using WhisperService.WorkerService.Channels;

namespace WhisperService.WorkerService;

public class TranscriptionWorker : BackgroundService
{
    private readonly ILogger<TranscriptionWorker> _logger;
    private readonly ITranscriptionService _transcriptionService;
    private readonly TranscriptionQueueChannel _channel;
    private readonly IAmazonS3 _s3Client;
    private readonly IOptions<S3Options> _s3Options;

    public TranscriptionWorker(
        ILogger<TranscriptionWorker> logger, ITranscriptionService transcriptionService, 
        TranscriptionQueueChannel channel, IAmazonS3 s3Client, IOptions<S3Options>  s3Options
        )
    {
        _logger = logger;
        _transcriptionService = transcriptionService;
        _channel = channel;
        _s3Client = s3Client;
        _s3Options = s3Options;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Transcription Worker running at: {time}", DateTimeOffset.Now);
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await foreach (var transcriptionJob in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation($"Starting Whisper transcription for: {transcriptionJob.S3Object.Key}");
                    var request = new GetObjectRequest
                    {
                        BucketName = _s3Options.Value.BucketName,
                        Key = transcriptionJob.S3Object.Key
                    };
                    
                    string fileName = Path.GetFileName(transcriptionJob.S3Object.Key);
                    string mediaFolder = Path.Combine(AppContext.BaseDirectory, "MediaFiles");
                    string filePath = Path.Combine(mediaFolder, fileName);
                    
                    using (GetObjectResponse response = await _s3Client.GetObjectAsync(request))
                    using (Stream responseStream = response.ResponseStream)
                    using (FileStream fileStream = File.Create(filePath))
                    {
                        await responseStream.CopyToAsync(fileStream);
                    }

                    if (!File.Exists(filePath))
                    {
                        throw new InvalidOperationException("Cannot find the file to  transcribe");
                    }

                    try
                    {
                        await using var stream = File.OpenRead(filePath);
                        await _transcriptionService.TranscribeAsync(stream);
                    }
                    finally
                    {
                        var deleteObjectRequest = new DeleteObjectRequest
                        {
                            BucketName = _s3Options.Value.BucketName,
                            Key = transcriptionJob.S3Object.Key
                        };
                        DeleteObjectResponse deleteObjectResponse = await _s3Client.DeleteObjectAsync(deleteObjectRequest);
                        _channel.CompleteProcessing(transcriptionJob.S3Object.Key);
                        File.Delete(filePath); 
                        _logger.LogInformation($"Deleted local file at: {filePath}");
                        _logger.LogInformation($"Deleted transcription for: {transcriptionJob.S3Object.Key}");
                        _logger.LogInformation($"Finished transcription for: {transcriptionJob.S3Object.Key}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process {transcriptionJob.S3Object.Key}");
                }
            }
            
            // var wavPath = Path.Combine(AppContext.BaseDirectory, "MediaFiles", "kennedy44100_converted.wav");
            // if (File.Exists(wavPath))
            // {
            //     await using var stream = File.OpenRead(wavPath);
            //     await _transcriptionService.TranscribeAsync(stream);
            // }
            // else
            // {
            //     Console.WriteLine("Cannot find the wav file");
            // }

            await Task.Delay(2500, stoppingToken);
        }
    }
}
