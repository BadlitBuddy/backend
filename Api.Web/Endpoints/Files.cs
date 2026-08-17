using Api.Application.Files.Queries.GetUploadPresignedUrl;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Web.Endpoints;

public class Files : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(Upload).AllowAnonymous();
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
