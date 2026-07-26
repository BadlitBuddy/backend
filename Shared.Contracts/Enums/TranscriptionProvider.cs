using System.ComponentModel;

namespace Shared.Contracts.Enums;

public enum TranscriptionProvider
{
    [Description("Whisper.net")] WhisperNet,
    [Description("Groq")] Groq,
    [Description("Cloudflare")] Cloudflare
}
