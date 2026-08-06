namespace Api.Application.Transcripts.Dtos;

public record TranscriptDownloadUrlDto(string FileName, string DownloadUrl, DateTime Expiry);
