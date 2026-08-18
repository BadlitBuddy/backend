using System.Runtime.CompilerServices;
using System.Text;
using Infrastructure.Dtos.TranscriptionProviderResults;
using Shared.Abstractions.Services;
using Shared.Contracts.Constants;
using Shared.Contracts.Dtos;
using Whisper.net;

namespace Infrastructure.Services;

public class WhisperTranscriptionService : IStreamingTranscriptionService, IDisposable
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

            _whisperFactory = WhisperFactory.FromPath(modelPath);
            _processor = _whisperFactory.CreateBuilder()
                .WithLanguage(LanguageOptions.Auto)
                .Build() ?? throw new InvalidOperationException("Failed to build WhisperProcessor.");
        }
        catch (Exception ex)
        {
            Dispose();
            throw new InvalidOperationException(
                $"Failed to initialize WhisperTranscriptionService with message: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        _processor.Dispose();
        _whisperFactory.Dispose();
    }

    public async IAsyncEnumerable<TranscriptionSegment> TranscribeStreamingAsync(
        Stream fileStream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var result in _processor.ProcessAsync(fileStream, cancellationToken))
        {
            yield return WhisperNetMapper.ToSegment(result);
        }
    }
}
