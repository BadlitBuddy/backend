using Shared.Contracts.Enums;

namespace Shared.Infrastructure.Configuration;

public class WorkerOptions
{
    public TranscriptionProvider? TranscriptionProvider { get; set; }
}
