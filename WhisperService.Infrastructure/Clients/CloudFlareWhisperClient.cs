using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Dtos.TranscriptionProviderResults;
using Infrastructure.Dtos.TranscriptionProviderResults.CloudFlare;
using Infrastructure.Helpers;
using WhisperService.Core.Dtos;

namespace Infrastructure.Clients;

public class CloudFlareWhisperClient
{
    private const string WhisperLargeV3TurboModelEndpoint = "ai/run/@cf/openai/whisper-large-v3-turbo";
    private const string WhisperTinyEnModelEndpoint = "ai/run/@cf/openai/whisper-tiny-en";
    private readonly HttpClient _httpClient;

    public CloudFlareWhisperClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CloudflareResponse<CfWhisperV3TurboResponse>?> TranscribeWhisperV3TurboAsync(
        Stream audioStream,
        WhisperV3TurboRequest request,
        CancellationToken cancellationToken = default)
    {
        using var content = new WhisperStreamingJsonContent(request, audioStream);

        using var response = await _httpClient.PostAsync(
            WhisperLargeV3TurboModelEndpoint,
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CloudflareResponse<CfWhisperV3TurboResponse>>(
            cancellationToken: cancellationToken);
    }

    public async Task<CloudflareResponse<CfWhisperTinyEnResponse>?> TranscribeWithWhisperTinyEnAsync(
        Stream audioStream,
        CancellationToken cancellationToken = default)
    {
        using var content = new StreamContent(audioStream, 64 * 1024);
        content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

        using var response = await _httpClient.PostAsync(
            WhisperTinyEnModelEndpoint,
            content,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<
            CloudflareResponse<CfWhisperTinyEnResponse>>(
            cancellationToken: cancellationToken);
    }
}
