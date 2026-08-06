using Api.Domain.Entities;
using Shared.Common.Helpers;

namespace Api.Application.Transcripts.Dtos;

public static class TranscriptMappingExtensions
{
    public static TranscriptDto ToDto(this TranscriptionJob entity)
    {
        return new TranscriptDto
        {
            Id = entity.PublicId,
            FileName = StoragePathBuilder.ExtractUnprocessedFileKeyParts(entity.UnprocessedObjectKey).originalName,
            JobStatus = entity.JobStatus,
            CreatedAt = entity.Created
        };
    }
}
