using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;

namespace Shared.Infrastructure.Jobs.TranscriptionJob.TranscriptionProviders;

public class CloudFlareTranscriber
{
    private readonly ILogger<CloudFlareTranscriber> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ITranscriptionService _cloudFlareTranscriptionService;
    private readonly IStreamableTranscriptionExporter _streamableTranscriptionExporter;
    private readonly IMessagePublisher _messagePublisher;

    public CloudFlareTranscriber(
        IHostEnvironment hostEnvironment, ILogger<CloudFlareTranscriber> logger,
        [FromKeyedServices(TranscriptionProvider.Cloudflare)] ITranscriptionService cloudFlareTranscriptionService,
        IStreamableTranscriptionExporter streamableTranscriptionExporter, IMessagePublisher messagePublisher)
    {
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _cloudFlareTranscriptionService = cloudFlareTranscriptionService;
        _streamableTranscriptionExporter = streamableTranscriptionExporter;
        _messagePublisher = messagePublisher;
    }

    public async Task Transcribe(string toProcessFilePath, string outputFilePath,
        string fileKey, CancellationToken cancellationToken)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("Transcribing with CLoudflare");
        }

        using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var transcriptionTask =
            _cloudFlareTranscriptionService.TranscribeAsync(
                new TranscriptionSource.FilePath(new FileInfo(toProcessFilePath)),
                TranscriptionModel.WhisperV3LargeTurbo,
                cancellationToken);
        var progressTask = TranscriptionJobHelpers.PublishProgressAsync(
            transcriptionTask,
            fileKey,
            _messagePublisher,
            progressCts.Token);

        TranscriptionResult transcriptionResult;
        try
        {
            transcriptionResult = await transcriptionTask;
        }
        finally
        {
            await progressCts.CancelAsync();
            await progressTask;
        }

        await using var fileStream = File.Create(outputFilePath);
        await _streamableTranscriptionExporter.ToJsonAsync(transcriptionResult, fileStream, cancellationToken);

        if (_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("Transcribed text: {Text}", transcriptionResult.Text);
        }
    }
}
