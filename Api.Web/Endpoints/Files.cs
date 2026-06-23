using Api.Application.Files.Queries.GetUploadPresignedUrl;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Web.Endpoints;

public class Files : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost("Upload", Upload);
    }

    [EndpointSummary("Upload")]
    [EndpointDescription("Get url to upload file")]
    public static async Task<Ok<UploadUrlDto>> Upload(ISender sender, GetUploadPresignedUrlQuery query)
    {
        var audioJobDto = await sender.Send(query);

        return TypedResults.Ok(audioJobDto);
    }
}