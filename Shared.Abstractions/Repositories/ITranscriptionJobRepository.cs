using Api.Domain.Dtos;
using Api.Domain.Entities;
using Api.Domain.Enums;

namespace Shared.Abstractions.Repositories;

public interface ITranscriptionJobRepository
{
    Task<TranscriptionJob?> GetByUnprocessedObjectKeyAsync(string unprocessedObjectKey, Guid userId);
    Task<TranscriptionJobDto?> UpdateStatusAsync(string unprocessedObjectKey, string? processedObjectKey, TranscriptionJobStatus status, Guid userId);
}
