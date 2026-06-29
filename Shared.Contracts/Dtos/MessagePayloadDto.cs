namespace Shared.Contracts.Dtos;

public sealed record TranscriptionFinishedMessage(
    string UserId,
    string UnprocessedWavFileObjectKey,
    string TranscriptionFileObjectKey);