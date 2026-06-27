using Api.Domain.Enums;

namespace Api.Application.Files.Commands.UpdateFileStatus;

public sealed record UpdateFileStatusResponse(
    string UnprocessedObjectKey,
    TranscriptionJobStatus JobStatus
)
{
    public string JobStatusName => JobStatus.GetDescription();
}