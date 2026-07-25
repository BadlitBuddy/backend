using System.Runtime.CompilerServices;
using System.Text;
using Shared.Abstractions.Services;
using Shared.Contracts.Constants;
using Shared.Infrastructure.Dtos.TranscriptionProviderResults;
using Whisper.net;

namespace Shared.Infrastructure;

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

    public async IAsyncEnumerable<TranscriptionSegment> TranscribeAsync(
        Stream fileStream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var result in _processor.ProcessAsync(fileStream, cancellationToken))
        {
            yield return WhisperNetMapper.ToSegment(result);
        }
    }

    public async Task TranscribeAndWriteAsSrtFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8);
        int counter = 1;

        await foreach (var segment in _processor.ProcessAsync(fileStream, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string startTime = segment.Start.ToString(@"hh\:mm\:ss\,fff");
            string endTime = segment.End.ToString(@"hh\:mm\:ss\,fff");

            await writer.WriteLineAsync(counter.ToString());
            await writer.WriteLineAsync($"{startTime} --> {endTime}");
            await writer.WriteLineAsync(segment.Text.Trim());
            await writer.WriteLineAsync();

            counter++;
        }
    }

    public async Task TranscribeAndWriteAsTxtFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8);

        await foreach (var segment in _processor.ProcessAsync(fileStream, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await writer.WriteLineAsync(segment.Text.Trim());
        }
    }

    public async Task TranscribeAndWriteAsVttFileAsync(Stream fileStream, string outputFilePath,
        CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8);

        await writer.WriteLineAsync("WEBVTT");
        await writer.WriteLineAsync();

        await foreach (var segment in _processor.ProcessAsync(fileStream, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string startTime = segment.Start.ToString(@"hh\:mm\:ss\.fff");
            string endTime = segment.End.ToString(@"hh\:mm\:ss\.fff");

            await writer.WriteLineAsync($"{startTime} --> {endTime}");
            await writer.WriteLineAsync(segment.Text.Trim());
            await writer.WriteLineAsync();
        }
    }
}
