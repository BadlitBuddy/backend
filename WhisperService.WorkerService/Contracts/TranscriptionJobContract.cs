using Shared.Contracts.Dtos;

namespace WhisperService.WorkerService.Contracts;

public record TranscriptionJobContract(AudioJobDto AudioJob);
