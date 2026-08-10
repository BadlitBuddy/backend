using Api.Application.Common.Models;
using Api.Application.Transcripts.Commands.Transcribe;
using Api.Application.Transcripts.Dtos;
using Api.Application.Transcripts.Querries;
using Api.Application.Transcripts.Querries.GetTranscriptDownloadUrl;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Web.Endpoints;

public class Transcripts : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(TranscribeFile).AllowAnonymous();
        groupBuilder.MapGet(GetTranscripts);
        groupBuilder.MapGet(GetTranscriptDownloadUrl, "{id}/download-url");
    }

    [EndpointSummary("Transcribe a file")]
    [EndpointDescription(
        "Transcribes an audio file given an object key, returning the details of the transcribed result upon success.")]
    public static async Task<Results<ProblemHttpResult, Ok<TranscribedFileResponse>>> TranscribeFile(
        [FromBody] TranscribeFileCommand command, ISender sender)
    {
        var result = await sender.Send(command);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Get Transcripts")]
    [EndpointDescription("Retrieves a paginated list of transcripts")]
    public static async Task<Results<ProblemHttpResult, Ok<PaginatedList<TranscriptDto>>>> GetTranscripts(
        [FromQuery] int page, [FromQuery] int limit, ISender sender)
    {
        var request = new GetTranscriptsQuery
        {
            Limit = limit,
            Page = page
        };

        var result = await sender.Send(request);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Get Transcript Download URL")]
    [EndpointDescription("Gets a presigned URL to download a specific transcript by ID")]
    public static async Task<Results<ProblemHttpResult, Ok<TranscriptDownloadUrlDto>>> GetTranscriptDownloadUrl(
        string id,
        ISender sender)
    {
        var request = new GetTranscriptDownloadUrlQuery
        {
            Id = id
        };

        var result = await sender.Send(request);

        if (result.IsFailure)
        {
            return result.ToProblemResult();
        }

        return TypedResults.Ok(result.Value);
    }
}
