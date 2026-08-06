using Api.Domain.Entities;
using Shared.Common.Helpers;

namespace Api.Application.Transcripts.Dtos;

public static class TranscriptMappingExtensions
{
    public static TranscriptDto ToDto(this TranscriptionJob entity)
    {
        return new TranscriptDto
        {
            UnprocessedObjectKey = entity.UnprocessedObjectKey,
            OriginalUnprocessedFileName = StoragePathBuilder.ExtractUnprocessedFileKeyParts(entity.UnprocessedObjectKey)
                .originalName,
            ProcessedObjectKey = entity.ProcessedObjectKey,
            OriginalProcessedFileName = !string.IsNullOrWhiteSpace(entity.ProcessedObjectKey)
                ? StoragePathBuilder.ExtractProcessedFileKeyParts(entity.ProcessedObjectKey).originalName
                : null,
            JobStatus = entity.JobStatus,
            CreatedAt = entity.Created
        };
    }
}
