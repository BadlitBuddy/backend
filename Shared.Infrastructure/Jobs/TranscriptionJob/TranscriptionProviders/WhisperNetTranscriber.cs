using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Services;
using Shared.Contracts.DtoMappers;
using Shared.Contracts.Enums;

namespace Shared.Infrastructure.Jobs.TranscriptionJob.TranscriptionProviders;

public class WhisperNetTranscriber
{
    private readonly IStreamingTranscriptionService _transcriptionService;
    private readonly ILogger<WhisperNetTranscriber> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IStreamableTranscriptionExporter _streamableTranscriptionExporter;

    public WhisperNetTranscriber(IHostEnvironment hostEnvironment, ILogger<WhisperNetTranscriber> logger,
        IStreamingTranscriptionService transcriptionService,
        IStreamableTranscriptionExporter streamableTranscriptionExporter, IMessagePublisher messagePublisher)
    {
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _transcriptionService = transcriptionService;
        _streamableTranscriptionExporter = streamableTranscriptionExporter;
        _messagePublisher = messagePublisher;
    }

    public async Task Transcribe(string toProcessFilePath, string outputFilePath, string fileKey,
        CancellationToken cancellationToken)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("Transcribing with Whisper.net");
        }

        await _messagePublisher.PublishAsync(MessageChannel.TranscriptionProcess,
            new TranscriptionProcessMessage(JobStatus.Processing, fileKey, null));

        await using var stream = File.OpenRead(toProcessFilePath);
        var segments = new List<TranscriptionSegment>();

        await foreach (var segment in _transcriptionService.TranscribeStreamingAsync(stream, cancellationToken))
        {
            segments.Add(segment);

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation($"{segment.Start}->{segment.End}: {segment.Text}");
            }

            await _messagePublisher.PublishAsync(
                MessageChannel.TranscriptionProcess,
                new TranscriptionProcessMessage(JobStatus.Processing, fileKey, null));
        }

        TranscriptionResult transcriptionResult = segments.ToTranscriptionResult(
            task: TranscriptionTask.Transcribe,
            providerModelId: TranscriptionProvider.WhisperNet
        );

        await using var fileStream = File.Create(outputFilePath);
        await _streamableTranscriptionExporter.ToJsonAsync(transcriptionResult, fileStream, cancellationToken);
    }
}
