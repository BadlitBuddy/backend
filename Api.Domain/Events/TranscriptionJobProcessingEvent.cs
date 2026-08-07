namespace Api.Domain.Events;

public class TranscriptionJobProcessingEvent : BaseEvent
{
    public TranscriptionJob TranscriptionJob { get; }

    public TranscriptionJobProcessingEvent(TranscriptionJob transcriptionJob)
    {
        TranscriptionJob = transcriptionJob;
    }
}
