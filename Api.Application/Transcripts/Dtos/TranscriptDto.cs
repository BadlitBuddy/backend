using Api.Domain.Enums;

namespace Api.Application.Transcripts.Dtos;

public class TranscriptDto
{
    public required string Id { get; set; }
    public required string FileName { get; set; }
    public TranscriptionJobStatus JobStatus { get; set; }
    public string JobStatusDesc => JobStatus.GetDescription();
    public required DateTimeOffset CreatedAt { get; set; }
}
