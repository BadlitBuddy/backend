using Api.Application.Common.Interfaces;
using Api.Application.Files.Commands.UpdateFileStatus;
using Api.Application.Files.Queries.GetUploadPresignedUrl;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;
using TranscriptionJobStatus = Api.Domain.Enums.TranscriptionJobStatus;

namespace Api.Web.Endpoints;

public record UpdateStatusRequest(string? ProcessedObjectKey, TranscriptionJobStatus TranscriptionJobStatus);

public record FileEventRequest(string[] UnprocessedObjectKeys);

public class Files : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(Upload).AllowAnonymous();
        // TODO: This is not  RESTful consolidate under Transcripts
        groupBuilder.MapPost("status", UpdateStatus);
        groupBuilder.MapPost("events", Events);
    }


    [EndpointSummary("Events")]
    [EndpointDescription("Events related to the files")]
    public async static Task<Results<ServerSentEventsResult<TranscriptionProcessMessage>, ProblemHttpResult>> Events(
        [FromBody] FileEventRequest request,
        IMessageSubscriber messageSubscriber, IUser currentUserService,
        IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var unProcessedObjectKeys = request.UnprocessedObjectKeys.Distinct();
        var transcriptionJobs = await dbContext.TranscriptionJobs
            .Where(tj =>
                tj.UserId == new Guid(currentUserService.Id!) && tj.JobStatus != TranscriptionJobStatus.Completed)
            .Where(tj => unProcessedObjectKeys.Contains(tj.UnprocessedObjectKey))
            .ToListAsync(cancellationToken: cancellationToken);

        if (transcriptionJobs.Count == 0)
        {
            return TypedResults.Problem(
                detail: "No files are associated with the provided keys.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource Not Found");
        }

        return TypedResults.ServerSentEvents(
            GetEvents(),
            eventType: "transcription-finished");

        async IAsyncEnumerable<TranscriptionProcessMessage> GetEvents()
        {
            await foreach (var message in messageSubscriber
                               .SubscribeAsync<TranscriptionProcessMessage>(
                                   MessageChannel.TranscriptionProcess)
                               .WithCancellation(cancellationToken))
            {
                var transcriptionJob = transcriptionJobs.SingleOrDefault(tj =>
                    message.UnprocessedWavFileObjectKey == tj.UnprocessedObjectKey);
                if (transcriptionJob != null)
                {
                    yield return message;
                }
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
