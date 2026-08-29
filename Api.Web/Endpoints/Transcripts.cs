using Api.Application.Common.Interfaces;
using Api.Application.Common.Models;
using Api.Application.Transcripts.Commands.Transcribe;
using Api.Application.Transcripts.Dtos;
using Api.Application.Transcripts.Querries;
using Api.Application.Transcripts.Querries.GetTranscript;
using Api.Application.Transcripts.Querries.GetTranscriptDownloadUrl;
using Api.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;

namespace Api.Web.Endpoints;

public class Transcripts : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(TranscribeFile).AllowAnonymous();
        groupBuilder.MapGet(GetTranscripts);
        groupBuilder.MapGet(GetTranscriptDownloadUrl, "{id}/download-url").AllowAnonymous();
        groupBuilder.MapGet(Events, "{id}/events").AllowAnonymous();
        groupBuilder.MapGet(GetTranscript, "{id}");
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

    [EndpointSummary("Get transcript by ID")]
    [EndpointDescription("Retrieves the full details and metadata of a specific transcription record using its unique identifier.")]
    public static async Task<Results<ProblemHttpResult, Ok<GetTranscriptResponse>>> GetTranscript(
        string id, [FromQuery] bool includeSrtSegments, [FromQuery] bool includeVttSegments, ISender sender)
    {
        var request = new GetTranscriptQuery
        {
            Id = id,
            IncludeSrtSegments = includeSrtSegments,
            IncludeVttSegments = includeVttSegments
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

    [EndpointSummary("Events")]
    [EndpointDescription("Events related to the files")]
    public async static Task<Results<ServerSentEventsResult<TranscriptionProcessMessage>, ProblemHttpResult>> Events(
        [FromRoute] string id, IMessageSubscriber messageSubscriber,
        IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var transcription = await dbContext.Transcripts
            .FirstOrDefaultAsync(tj =>
                tj.JobStatus != TranscriptionJobStatus.Completed &&
                tj.PublicId == id, cancellationToken: cancellationToken);

        if (transcription == null)
        {
            return TypedResults.Problem(
                detail: "No transcription is associated with the provided id.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource Not Found");
        }

        return TypedResults.ServerSentEvents(
            GetEvents(),
            eventType: "transcription-event");

        async IAsyncEnumerable<TranscriptionProcessMessage> GetEvents()
        {
            await foreach (var message in messageSubscriber
                               .SubscribeAsync<TranscriptionProcessMessage>(
                                   MessageChannel.TranscriptionProcess)
                               .WithCancellation(cancellationToken))
            {
                if (transcription.UnprocessedObjectKey != message.UnprocessedWavFileObjectKey)
                    continue;

                yield return message;

                if (message.JobStatus == JobStatus.Finished)
                    yield break;
            }
        }
    }
}
