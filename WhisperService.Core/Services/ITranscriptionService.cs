namespace WhisperService.Core.Services;

public interface ITranscriptionService
{
    public Task TranscribeAsync(Stream fileStream);
}