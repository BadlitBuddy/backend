namespace Api.Domain.Dtos;

public class TranscriptionJobDto
{
    public Guid UserId { get; set; }
    public required string UnprocessedObjectKey { get; set; }
    public string? ProcessedObjectKey { get; set; }
    public TranscriptionJobStatus JobStatus { get; set; }
}

public class TranscriptionJobSummaryDto
{
    public required string UnprocessedObjectKey { get; set; }
    public string? ProcessedObjectKey { get; set; }
    public TranscriptionJobStatus JobStatus { get; set; }
}
