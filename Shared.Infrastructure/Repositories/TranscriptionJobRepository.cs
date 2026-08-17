using Api.Domain.Dtos;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Dapper;
using Shared.Abstractions.Repositories;
using Shared.Infrastructure.Data;

namespace Shared.Infrastructure.Repositories;

public class TranscriptionJobRepository : ITranscriptionJobRepository
{
    private readonly DapperDbContext _dbContext;

    public TranscriptionJobRepository(DapperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TranscriptionJob?> GetByUnprocessedObjectKeyAsync(string unprocessedObjectKey, Guid userId)
    {
        const string sql = @"
            SELECT * FROM public.""TranscriptionJobs""
            WHERE ""UnprocessedObjectKey"" = @UnprocessedObjectKey 
            AND ""UserId"" = @UserId";

        using var connection = _dbContext.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<TranscriptionJob>(sql, new
        {
            UnprocessedObjectKey = unprocessedObjectKey,
            UserId = userId
        });
    }

    public async Task<TranscriptionJobDto?> UpdateStatusAsync(
        string unprocessedObjectKey,
        string? processedObjectKey,
        TranscriptionJobStatus status,
        Guid userId)
    {
        const string sql = @"
                           UPDATE public.""TranscriptionJobs""
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

        var result = await connection.QuerySingleOrDefaultAsync<TranscriptionJobDto>(
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

    public async Task<TranscriptionJobSummaryDto?> UpdateProcessedObjectKeyAsync(string unprocessedObjectKey,
        string processedObjectKey,
        TranscriptionJobStatus status)
    {
        const string sql = @"
                           UPDATE public.""TranscriptionJobs""
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

        var result = await connection.QuerySingleOrDefaultAsync<TranscriptionJobSummaryDto>(
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
