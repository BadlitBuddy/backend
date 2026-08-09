using Api.Domain.Events;

namespace Api.Domain.Entities;

// TODO: rename this entity to 'Transcripts'
public class TranscriptionJob : BaseAuditableEntity<int>
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public required string UnprocessedObjectKey { get; set; }
    public string? ProcessedObjectKey { get; set; }
    public TranscriptionJobStatus JobStatus { get; private set; }
    public TimeSpan Duration { get; set; }

    public void MarkAsUploaded()
    {
        if (ProcessedObjectKey != null)
        {
            throw new InvalidOperationException("Cannot set as uploaded since file is already processed");
        }

        JobStatus = TranscriptionJobStatus.Uploaded;
        AddDomainEvent(new TranscriptionJobUploadedEvent(this));
    }

    public void MarkAsProcessing()
    {
        if (ProcessedObjectKey != null)
        {
            throw new InvalidOperationException("Cannot  set as processing since file is already processed");
        }

        JobStatus = TranscriptionJobStatus.Processing;
        AddDomainEvent(new TranscriptionJobProcessingEvent(this));
    }

    public void MarkAsCompleted()
    {
        if (ProcessedObjectKey == null)
        {
            throw new InvalidOperationException("Cannot set as completed since there is no processed file");
        }

        JobStatus = TranscriptionJobStatus.Completed;
    }
}
