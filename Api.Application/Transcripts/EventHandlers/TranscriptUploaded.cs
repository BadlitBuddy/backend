using Api.Domain.Events;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Jobs;
using Shared.Abstractions.Services;

namespace Api.Application.Transcripts.EventHandlers;

public class TranscriptUploaded : INotificationHandler<TranscriptUploadedEvent>
{
    private readonly ILogger<TranscriptUploaded> _logger;
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly ITranscriptionJobScheduler _transcriptionJobScheduler;

    public TranscriptUploaded(ILogger<TranscriptUploaded> logger,
        IAudioJobStorageService audioJobStorageService, ITranscriptionJobScheduler transcriptionJobScheduler)
    {
        _logger = logger;
        _audioJobStorageService = audioJobStorageService;
        _transcriptionJobScheduler = transcriptionJobScheduler;
    }

    public async Task Handle(TranscriptUploadedEvent notification, CancellationToken cancellationToken)
    {
        if (!await _audioJobStorageService.IsStorageAvailableAsync(cancellationToken))
        {
            _logger.LogError("Could not access audio job storage.");
            throw new InvalidOperationException("Storage backend is unreachable.");
        }

        try
        {
            var result = _transcriptionJobScheduler.EnqueueTranscriptionJob(
                notification.Transcript.UnprocessedObjectKey,
                cancellationToken);
            _logger.LogInformation(
                "Job {Result} is being processed with object key:  {TranscriptionJobUnprocessedObjectKey}", result,
                notification.Transcript.UnprocessedObjectKey);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled error occurred in the TranscriptionJobUploaded event handler.");
            throw;
        }
    }
}
