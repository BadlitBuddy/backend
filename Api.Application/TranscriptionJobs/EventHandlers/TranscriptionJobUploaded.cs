using Api.Domain.Events;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.ExternalServices.S3;
using Shared.Abstractions.Jobs;

namespace Api.Application.TranscriptionJobs.EventHandlers;

public class TranscriptionJobUploaded : INotificationHandler<TranscriptionJobUploadedEvent>
{
    private readonly ILogger<TranscriptionJobUploaded> _logger;
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly ITranscriptionJobScheduler _transcriptionJobScheduler;

    public TranscriptionJobUploaded(ILogger<TranscriptionJobUploaded> logger,
        IAudioJobStorageService audioJobStorageService, ITranscriptionJobScheduler transcriptionJobScheduler)
    {
        _logger = logger;
        _audioJobStorageService = audioJobStorageService;
        _transcriptionJobScheduler = transcriptionJobScheduler;
    }

    public async Task Handle(TranscriptionJobUploadedEvent notification, CancellationToken cancellationToken)
    {
        if (!await _audioJobStorageService.IsStorageAvailableAsync(cancellationToken))
        {
            _logger.LogError("Could not access audio job storage.");
            throw new InvalidOperationException("Storage backend is unreachable.");
        }

        try
        {
            var result = _transcriptionJobScheduler.EnqueueTranscriptionJob(
                notification.TranscriptionJob.UnprocessedObjectKey,
                cancellationToken);
            _logger.LogInformation(
                "Job {Result} is being processed with object key:  {TranscriptionJobUnprocessedObjectKey}", result,
                notification.TranscriptionJob.UnprocessedObjectKey);
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