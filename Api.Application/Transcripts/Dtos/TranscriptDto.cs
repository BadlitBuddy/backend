using Api.Domain.Enums;

namespace Api.Application.Transcripts.Dtos;

public class TranscriptDto
{
    public required string UnprocessedObjectKey { get; set; }
    public required string OriginalUnprocessedFileName { get; set; }

    public string? ProcessedObjectKey { get; set; }
    public string? OriginalProcessedFileName { get; set; }

    public TranscriptionJobStatus JobStatus { get; set; }
    public string JobStatusDesc => JobStatus.GetDescription();
}
