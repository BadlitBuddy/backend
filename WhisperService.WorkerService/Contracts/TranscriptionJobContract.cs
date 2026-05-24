using WhisperService.Core.Dtos;

namespace WhisperService.WorkerService.Contracts;

public record TranscriptionJobContract(AudioJobDto AudioJob);
