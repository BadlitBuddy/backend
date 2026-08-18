using Api.Application.Common.Interfaces;
using Api.Application.Transcripts.Dtos;

namespace Api.Application.Transcripts.Querries;

public class GetTranscriptsQuery : IRequest<Result<PaginatedList<TranscriptDto>>>
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 5;
}

public class GetTranscriptsHandler : IRequestHandler<GetTranscriptsQuery, Result<PaginatedList<TranscriptDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUser _currentUser;

    public GetTranscriptsHandler(IApplicationDbContext dbContext, IUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PaginatedList<TranscriptDto>>> Handle(GetTranscriptsQuery request,
        CancellationToken cancellationToken)
    {
        var transcriptionJobs = await _dbContext.Transcripts
            .Where(tj => tj.UserId == Guid.Parse(_currentUser.Id!))
            .OrderByDescending(tj => tj.Created)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var totalTranscriptionJobs = await _dbContext.Transcripts
            .Where(tj => tj.UserId == Guid.Parse(_currentUser.Id!))
            .CountAsync(cancellationToken: cancellationToken);

        var transcriptionJobsDto = transcriptionJobs.Select(tj => tj.ToDto()).ToList();

        var list = new PaginatedList<TranscriptDto>(transcriptionJobsDto, totalTranscriptionJobs, request.Page,
            request.Limit);

        return Result<PaginatedList<TranscriptDto>>.Success(list);
    }
}
