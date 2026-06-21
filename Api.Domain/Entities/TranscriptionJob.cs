namespace Api.Domain.Entities;

public class TranscriptionJob : BaseAuditableEntity<int>
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string UnprocessedObjectKey { get; set; }
    public string? ProcessedObjectKey { get; set; }
    public TranscriptionJobStatus JobStatus { get; set; }
}