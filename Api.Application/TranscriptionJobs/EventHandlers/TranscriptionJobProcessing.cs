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
        var fileDuration =
            await _audioJobStorageService.GetWavDurationAsync(notification.TranscriptionJob.UnprocessedObjectKey,
                cancellationToken) ?? TimeSpan.Zero;

        notification.TranscriptionJob.Duration = fileDuration;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
