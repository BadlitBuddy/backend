using Api.Domain.Entities;
using Shared.Abstractions.Storage;

namespace Api.Application.Transcripts.Dtos;

public static class TranscriptMappingExtensions
{
    public static TranscriptDto ToDto(
        this TranscriptionJob entity,
        IStoragePathBuilder storagePathBuilder)
    {
        return new TranscriptDto
        {
            UnprocessedObjectKey = entity.UnprocessedObjectKey,
            OriginalUnprocessedFileName = storagePathBuilder.ExtractUnprocessedFileKeyParts(entity.UnprocessedObjectKey)
                .originalName,
            ProcessedObjectKey = entity.ProcessedObjectKey,
            OriginalProcessedFileName = !string.IsNullOrWhiteSpace(entity.ProcessedObjectKey)
                ? storagePathBuilder.ExtractProcessedFileKeyParts(entity.ProcessedObjectKey).originalName
                : null,
            JobStatus = entity.JobStatus
        };
    }
}
