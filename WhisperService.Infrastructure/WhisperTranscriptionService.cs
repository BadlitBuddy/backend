using WhisperService.Core.Services;
using Whisper.net;
using WhisperService.Core.Constants;

namespace Infrastructure;

public class WhisperTranscriptionService : ITranscriptionService, IDisposable
{
    private readonly WhisperProcessor _processor;
    private readonly WhisperFactory _whisperFactory;
    
    public WhisperTranscriptionService()
    {

        try
        {
            var modelPath = Path.Combine(AppContext.BaseDirectory, "Models", ModelOptions.LargeV3TurboQ5_0);
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException("Model file not found.");
            }
            else
            {
                Console.WriteLine("Model is found");
            }
            
            _whisperFactory = WhisperFactory.FromPath(modelPath);
            _processor = _whisperFactory.CreateBuilder()
                .WithLanguage(LanguageOptions.Auto)
                .Build() ?? throw new InvalidOperationException("Failed to build WhisperProcessor.");
        }
        catch (Exception ex)
        {
            Dispose();
            Console.WriteLine(ex.Message);
            throw new InvalidOperationException("Failed to initialize WhisperTranscriptionService.");
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _processor?.Dispose();
        _whisperFactory?.Dispose();
    }

    public async Task TranscribeAsync(Stream fileStream)
    {
        await foreach (var result in _processor.ProcessAsync(fileStream))
        {
            Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");
        }
    }
}