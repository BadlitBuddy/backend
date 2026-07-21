namespace Shared.Contracts.Enums;

public class MessageChannel
{
    private readonly string _channel;

    private MessageChannel(string channel)
    {
        _channel = channel;
    }

    public static readonly MessageChannel TranscriptionProcess = new("transcription.process");

    public override string ToString() => _channel;

    public static implicit operator string(MessageChannel plan)
    {
        return plan._channel;
    }
}
