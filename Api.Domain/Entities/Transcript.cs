using Api.Domain.Events;

namespace Api.Domain.Entities;

public class Transcript : BaseAuditableEntity<int>
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
        AddDomainEvent(new TranscriptUploadedEvent(this));
    }

    public void MarkAsProcessing()
    {
        if (ProcessedObjectKey != null)
        {
            throw new InvalidOperationException("Cannot  set as processing since file is already processed");
        }

        JobStatus = TranscriptionJobStatus.Processing;
        AddDomainEvent(new TranscriptProcessingEvent(this));
    }

    public void MarkAsCompleted()
    {
        if (ProcessedObjectKey == null)
        {
            throw new InvalidOperationException("Cannot set as completed since there is no processed file");
        }

        JobStatus = TranscriptionJobStatus.Completed;
    }

    public void MarkAsCanceled()
    {
        JobStatus = TranscriptionJobStatus.Canceled;
    }

    public void MarkAsFailed()
    {
        JobStatus = TranscriptionJobStatus.Failed;
    }
}
