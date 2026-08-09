using Api.Application.Common.Interfaces;
using Api.Domain.Entities;
using Api.Domain.Enums;

namespace Api.Application.Files.Commands.UpdateFileStatus;

public class UpdateFileStatusCommand : IRequest<Result<UpdateFileStatusResponse>>
{
    public string UnprocessedObjectKey { get; set; } = string.Empty;
    public string? ProcessedObjectKey { get; set; } = string.Empty;
    public TranscriptionJobStatus TranscriptionJobStatus { get; set; }
}

public class UpdateFileStatusHandler : IRequestHandler<UpdateFileStatusCommand, Result<UpdateFileStatusResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUser _currentUserService;

    public UpdateFileStatusHandler(IApplicationDbContext dbContext, IUser currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UpdateFileStatusResponse>> Handle(UpdateFileStatusCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = new Guid(_currentUserService.Id!);
        // todo, use transcription job public id
        var transcriptionJob =
            await _dbContext.TranscriptionJobs.SingleOrDefaultAsync(tb =>
                tb.UnprocessedObjectKey == request.UnprocessedObjectKey &&
                tb.UserId == currentUserId, cancellationToken: cancellationToken);

        if (transcriptionJob == null && request.TranscriptionJobStatus != TranscriptionJobStatus.Uploaded)
        {
            Result<UpdateFileStatusResponse>.Failure([
                "No Job found", "You can only set a file's status as Uploaded if no corresponding file has no job"
            ]);
        }

        if (transcriptionJob == null)
        {
            var newTranscriptionJob = new TranscriptionJob
            {
                UserId = currentUserId,
                UnprocessedObjectKey = request.UnprocessedObjectKey
            };
            newTranscriptionJob.MarkAsUploaded();
            _dbContext.TranscriptionJobs.Add(newTranscriptionJob);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result<UpdateFileStatusResponse>.Success(
                new UpdateFileStatusResponse(newTranscriptionJob.UnprocessedObjectKey, newTranscriptionJob.JobStatus));
        }

        switch (request.TranscriptionJobStatus)
        {
            case TranscriptionJobStatus.Processing:
                transcriptionJob.MarkAsProcessing();
                break;
            case TranscriptionJobStatus.Completed:
                transcriptionJob.ProcessedObjectKey = request.ProcessedObjectKey;
                transcriptionJob.MarkAsCompleted();
                break;
            case TranscriptionJobStatus.Uploaded:
                break;
            default:
                return Result<UpdateFileStatusResponse>.Success(
                    new UpdateFileStatusResponse(transcriptionJob.UnprocessedObjectKey, transcriptionJob.JobStatus));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateFileStatusResponse>.Success(
            new UpdateFileStatusResponse(transcriptionJob.UnprocessedObjectKey, transcriptionJob.JobStatus));
    }
}
