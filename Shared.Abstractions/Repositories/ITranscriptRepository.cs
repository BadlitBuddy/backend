using Api.Domain.Dtos;
using Api.Domain.Entities;
using Api.Domain.Enums;

namespace Shared.Abstractions.Repositories;

public interface ITranscriptRepository
{
    Task<Transcript?> GetByUnprocessedObjectKeyAsync(string unprocessedObjectKey, Guid userId);

    Task<Transcript?> GetByIdAsync(int id);

    Task<TranscriptDto?> UpdateStatusAsync(string unprocessedObjectKey, string? processedObjectKey,
        TranscriptionJobStatus status, Guid userId);

    Task<TranscriptSummaryDto?> UpdateProcessedObjectKeyAsync(string unprocessedObjectKey,
        string processedObjectKey,
        TranscriptionJobStatus status);
}
