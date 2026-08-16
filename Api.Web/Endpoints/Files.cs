using Api.Application.Common.Interfaces;
using Api.Application.Files.Commands.UpdateFileStatus;
using Api.Application.Files.Queries.GetUploadPresignedUrl;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TranscriptionJobStatus = Api.Domain.Enums.TranscriptionJobStatus;

namespace Api.Web.Endpoints;

public record UpdateStatusRequest(string? ProcessedObjectKey, TranscriptionJobStatus TranscriptionJobStatus);

public class Files : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(Upload).AllowAnonymous();
        // TODO: This is not  RESTful consolidate under Transcripts
        groupBuilder.MapPost("status", UpdateStatus);
    }

    [EndpointSummary("Update Status")]
    [EndpointDescription("Update the status of the file")]
    public static async Task<Results<ProblemHttpResult, Ok<UpdateFileStatusResponse>>> UpdateStatus(ISender sender,
        [FromQuery] string objectKey, UpdateStatusRequest request)
    {
        var result = await sender.Send(new UpdateFileStatusCommand
        {
            UnprocessedObjectKey = objectKey, ProcessedObjectKey = request.ProcessedObjectKey,
            TranscriptionJobStatus = request.TranscriptionJobStatus
        });

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Upload")]
    [EndpointDescription("Get url to upload file")]
    public static async Task<Results<ProblemHttpResult, Ok<UploadUrlDto>>> Upload(ISender sender,
        GetUploadPresignedUrlQuery query)
    {
        var result = await sender.Send(query);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return TypedResults.Ok(result.Value);
    }
}
