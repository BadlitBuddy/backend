using Api.Application.Transcripts.Dtos;
using Shared.Contracts.Dtos;

namespace Api.Application.Transcripts.Querries.GetTranscript;

public class GetTranscriptResponse
{
    public required TranscriptDto Transcript { get; set; }
    public required TranscriptionResult JsonData { get; set; }
}
