using Api.Application.Common.Interfaces;
using Api.Domain.Events;
using Shared.Abstractions.Services;

namespace Api.Application.TranscriptionJobs.EventHandlers;

public class TranscriptionJobProcessing : INotificationHandler<TranscriptionJobProcessingEvent>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAudioJobStorageService _audioJobStorageService;

    public TranscriptionJobProcessing(IApplicationDbContext dbContext, IAudioJobStorageService audioJobStorageService)
    {
        _dbContext = dbContext;
        _audioJobStorageService = audioJobStorageService;
    }

    public async Task Handle(TranscriptionJobProcessingEvent notification, CancellationToken cancellationToken)
    {
        var transcriptionJob =
            await _dbContext.TranscriptionJobs.SingleOrDefaultAsync(tb =>
                tb.PublicId == notification.TranscriptionJob.PublicId, cancellationToken: cancellationToken);

        if (transcriptionJob == null)
        {
            throw new NotFoundException("Transcription", "Transcription job not found");
        }

        var fileDuration =
            await _audioJobStorageService.GetWavDurationAsync(notification.TranscriptionJob.UnprocessedObjectKey,
                cancellationToken) ?? TimeSpan.Zero;

        transcriptionJob.Duration = fileDuration;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
