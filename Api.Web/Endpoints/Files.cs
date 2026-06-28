using Api.Application.Files.Commands.UpdateFileStatus;
using Api.Application.Files.Queries.GetUploadPresignedUrl;
using Api.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;

namespace Api.Web.Endpoints;

public record UpdateStatusRequest(string? ProcessedObjectKey, TranscriptionJobStatus TranscriptionJobStatus);

public class Files : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(Upload);
        groupBuilder.MapPost("status", UpdateStatus);
        groupBuilder.MapGet("events", Events);
    }


    [EndpointSummary("Events")]
    [EndpointDescription("Events related to the files")]
    public static ServerSentEventsResult<TranscriptionFinishedMessage> Events(
        [FromQuery] string unProcessedObjectKey,
        IMessageSubscriber messageSubscriber,
        CancellationToken cancellationToken)
    {
        return TypedResults.ServerSentEvents(
            GetEvents(),
            eventType: "transcription-finished");

        async IAsyncEnumerable<TranscriptionFinishedMessage> GetEvents()
        {
            await foreach (var message in messageSubscriber
                               .SubscribeAsync<TranscriptionFinishedMessage>(
                                   MessageChannel.TranscriptionFinished)
                               .WithCancellation(cancellationToken))
            {
                if (message.UnprocessedWavFileObjectKey != unProcessedObjectKey)
                {
                    continue;
                }

                yield return message;
            }
        }
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