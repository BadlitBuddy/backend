using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos;

public sealed record TranscriptionProcessMessage(
    JobStatus JobStatus,
    string UnprocessedWavFileObjectKey,
    string? TranscriptionFileObjectKey);
