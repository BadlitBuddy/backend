using Api.Application.Files.Commands.UpdateFileStatus;
using Api.Application.Files.Queries.GetUploadPresignedUrl;
using Api.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Web.Endpoints;

public record UpdateStatusRequest(string? ProcessedObjectKey, TranscriptionJobStatus TranscriptionJobStatus);

public class Files : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(Upload);
        groupBuilder.MapPost("{objectKey}/status", UpdateStatus);
    }

    [EndpointSummary("Upload")]
    [EndpointDescription("Get url to upload file")]
    public static async Task<Results<ProblemHttpResult, Ok<UpdateFileStatusResponse>>> UpdateStatus(ISender sender,
        string objectKey, UpdateStatusRequest request)
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