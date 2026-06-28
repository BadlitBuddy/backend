namespace Shared.Contracts.Dtos;

public sealed record TranscriptionFinishedMessage(
    string UnprocessedWavFileObjectKey,
    string TranscriptionFileObjectKey);