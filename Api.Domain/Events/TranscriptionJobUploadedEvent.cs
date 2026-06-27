namespace Api.Domain.Events;

public class TranscriptionJobUploadedEvent : BaseEvent
{
    public TranscriptionJob TranscriptionJob { get; }

    public TranscriptionJobUploadedEvent(TranscriptionJob transcriptionJob)
    {
        TranscriptionJob = transcriptionJob;
    }
}