using Api.Application.Common.Enums;
using Api.Application.Common.Interfaces;
using Api.Application.Transcripts.Dtos;
using Api.Domain.Enums;
using Shared.Abstractions.Services;

namespace Api.Application.Transcripts.Querries.GetTranscriptDownloadUrl;

public class GetTranscriptDownloadUrlQuery : IRequest<Result<TranscriptDownloadUrlDto>>
{
    public required string Id { get; set; }
}

public class
    GetTranscriptDownloadUrlHandler : IRequestHandler<GetTranscriptDownloadUrlQuery, Result<TranscriptDownloadUrlDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAudioJobStorageService _audioJobStorageService;
    private readonly IUser _currentUserService;

    public GetTranscriptDownloadUrlHandler(IApplicationDbContext dbContext,
        IAudioJobStorageService audioJobStorageService, IUser currentUserService)
    {
        _dbContext = dbContext;
        _audioJobStorageService = audioJobStorageService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TranscriptDownloadUrlDto>> Handle(GetTranscriptDownloadUrlQuery request,
        CancellationToken cancellationToken)
    {
        var transcript = await _dbContext.TranscriptionJobs
            .FirstOrDefaultAsync(
                tj => tj.PublicId == request.Id &&
                      tj.JobStatus == TranscriptionJobStatus.Completed &&
                      tj.ProcessedObjectKey != null, cancellationToken: cancellationToken);

        if (transcript == null)
        {
            return Result<TranscriptDownloadUrlDto>.Failure([
                "No transcript found, You can only download processed/completed files"
            ]);
        }

        if (transcript.ProcessedObjectKey == null)
        {
            return Result<TranscriptDownloadUrlDto>.Failure([
                "Transcript has not been processed yet, You can only download processed/completed files"
            ]);
        }

        DateTime expiry = _currentUserService.UserTier switch
        {
            UserTier.Free => DateTime.UtcNow.AddHours(1),
            UserTier.Public => DateTime.UtcNow.AddMinutes(10),
            _ => DateTime.UtcNow.AddMinutes(30)
        };

        var downloadData =
            await _audioJobStorageService.CreateDownloadUrlAsync(transcript.ProcessedObjectKey, expiry,
                cancellationToken);

        return Result<TranscriptDownloadUrlDto>.Success(transcript.ToDownloadUrlDto(downloadData.uri.AbsoluteUri,
            downloadData.expiry));
    }
}
