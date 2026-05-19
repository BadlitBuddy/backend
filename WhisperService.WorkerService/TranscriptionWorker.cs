using WhisperService.Core.Services;

namespace WhisperService.WorkerService;

public class TranscriptionWorker(ILogger<TranscriptionWorker> logger, ITranscriptionService transcriptionService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            
            var wavPath = Path.Combine(AppContext.BaseDirectory, "TestData", "kennedy44100_converted.wav");
            if (File.Exists(wavPath))
            {
                await using var stream = File.OpenRead(wavPath);
                await transcriptionService.TranscribeAsync(stream);
            }
            else
            {
                Console.WriteLine("Cannot find the wav file");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}
