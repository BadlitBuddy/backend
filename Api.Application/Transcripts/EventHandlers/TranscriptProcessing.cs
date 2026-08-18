using Api.Application.Common.Interfaces;
using Api.Domain.Events;
using Shared.Abstractions.Services;

namespace Api.Application.Transcripts.EventHandlers;

public class TranscriptProcessing : INotificationHandler<TranscriptProcessingEvent>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAudioJobStorageService _audioJobStorageService;

    public TranscriptProcessing(IApplicationDbContext dbContext, IAudioJobStorageService audioJobStorageService)
    {
        _dbContext = dbContext;
        _audioJobStorageService = audioJobStorageService;
    }

    public async Task Handle(TranscriptProcessingEvent notification, CancellationToken cancellationToken)
    {
        var fileDuration =
            await _audioJobStorageService.GetWavDurationAsync(notification.Transcript.UnprocessedObjectKey,
                cancellationToken) ?? TimeSpan.Zero;

        notification.Transcript.Duration = fileDuration;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
