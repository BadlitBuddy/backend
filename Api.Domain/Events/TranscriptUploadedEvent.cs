namespace Api.Domain.Events;

public class TranscriptUploadedEvent : BaseEvent
{
    public Transcript Transcript { get; }

    public TranscriptUploadedEvent(Transcript transcript)
    {
        Transcript = transcript;
    }
}
