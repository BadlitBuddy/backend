namespace Api.Domain.Events;

public class TranscriptProcessingEvent : BaseEvent
{
    public Transcript Transcript { get; }

    public TranscriptProcessingEvent(Transcript transcript)
    {
        Transcript = transcript;
    }
}
