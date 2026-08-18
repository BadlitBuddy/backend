using Api.Domain.Entities;
using Shared.Common.Helpers;

namespace Api.Application.Transcripts.Dtos;

public static class TranscriptMappingExtensions
{
    public static TranscriptDto ToDto(this Transcript entity)
    {
        return new TranscriptDto
        {
            Id = entity.PublicId,
            FileName = StoragePathBuilder.ExtractUnprocessedFileKeyParts(entity.UnprocessedObjectKey).originalName,
            JobStatus = entity.JobStatus,
            CreatedAt = entity.Created,
            Duration = entity.Duration
        };
    }

    public static TranscriptDownloadUrlDto ToDownloadUrlDto(this Transcript entity, string downloadUrl,
        DateTime expiry)
    {
        var fileName = StoragePathBuilder.ExtractUnprocessedFileKeyParts(entity.UnprocessedObjectKey).originalName;
        return new TranscriptDownloadUrlDto(fileName, downloadUrl, expiry);
    }
}
