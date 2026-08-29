using Api.Application.Transcripts.Dtos;
using Shared.Contracts.Dtos;

namespace Api.Application.Transcripts.Querries.GetTranscript;

public class GetTranscriptResponse
{
    public required TranscriptDto Transcript { get; set; }
    public required TranscriptionResult JsonData { get; set; }
    public required MetaData MetaData { get; set; }
    public List<SrtSegment>? SrtSegments { get; set; }
    public List<VttSegment>? VttSegments { get; set; }
}

public record MetaData(int wordCount, string duration, string language);
