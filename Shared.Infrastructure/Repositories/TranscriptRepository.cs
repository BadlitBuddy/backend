using Api.Domain.Dtos;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Dapper;
using Shared.Abstractions.Repositories;
using Shared.Infrastructure.Data;

namespace Shared.Infrastructure.Repositories;

public class TranscriptRepository : ITranscriptRepository
{
    private readonly DapperDbContext _dbContext;

    public TranscriptRepository(DapperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Transcript?> GetByUnprocessedObjectKeyAsync(string unprocessedObjectKey, Guid userId)
    {
        const string sql = @"
            SELECT * FROM public.""Transcripts""
            WHERE ""UnprocessedObjectKey"" = @UnprocessedObjectKey 
            AND ""UserId"" = @UserId";

        using var connection = _dbContext.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<Transcript>(sql, new
        {
            UnprocessedObjectKey = unprocessedObjectKey,
            UserId = userId
        });
    }

    public async Task<Transcript?> GetByIdAsync(int id)
    {
        const string sql = """
                               SELECT * FROM public."Transcripts"
                               WHERE "Id" = @Id 
                           """;

        using var connection = _dbContext.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<Transcript>(sql, new
        {
            Id = id
        });
    }

    public async Task<TranscriptDto?> UpdateStatusAsync(
        string unprocessedObjectKey,
        string? processedObjectKey,
        TranscriptionJobStatus status,
        Guid userId)
    {
        const string sql = @"
                           UPDATE public.""Transcripts""
                           SET
                               ""ProcessedObjectKey"" = @ProcessedObjectKey,
                               ""JobStatus""            = @JobStatus,
                               ""LastModifiedBy""      = @UserId,
                               ""LastModified""         = NOW()
                           WHERE ""UnprocessedObjectKey"" = @UnprocessedObjectKey
                             AND ""UserId""               = @UserId
                           RETURNING
                               ""UserId"",
                               ""UnprocessedObjectKey"",
                               ""ProcessedObjectKey"",
                               ""JobStatus""
                               ";

        using var connection = _dbContext.CreateConnection();

        var result = await connection.QuerySingleOrDefaultAsync<TranscriptDto>(
            sql,
            new
            {
                UnprocessedObjectKey = unprocessedObjectKey,
                ProcessedObjectKey = processedObjectKey,
                JobStatus = (int)status,
                UserId = userId,
            });

        return result;
    }

    public async Task<TranscriptSummaryDto?> UpdateProcessedObjectKeyAsync(string unprocessedObjectKey,
        string processedObjectKey,
        TranscriptionJobStatus status)
    {
        const string sql = @"
                           UPDATE public.""Transcripts""
                           SET
                               ""ProcessedObjectKey"" = @ProcessedObjectKey,
                               ""JobStatus""            = @JobStatus,
                               ""LastModified""         = NOW()
                           WHERE ""UnprocessedObjectKey"" = @UnprocessedObjectKey
                           RETURNING
                               ""UnprocessedObjectKey"",
                               ""ProcessedObjectKey"",
                               ""JobStatus""
                               ";

        using var connection = _dbContext.CreateConnection();

        var result = await connection.QuerySingleOrDefaultAsync<TranscriptSummaryDto>(
            sql,
            new
            {
                UnprocessedObjectKey = unprocessedObjectKey,
                ProcessedObjectKey = processedObjectKey,
                JobStatus = (int)status
            });

        return result;
    }
}
