using System.Net.Http.Json;
using Infrastructure.Dtos.TranscriptionProviderResults;

namespace Infrastructure.Clients;

public class GroqWhisperClient
{
    private const string Model = "whisper-large-v3-turbo";

    private const string
        Endpoint = "audio/transcriptions";

    private readonly HttpClient _httpClient;

    public GroqWhisperClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Transcribes an audio file referenced by a publicly-accessible URL.</summary>
    public async Task<GroqVerboseResponse> TranscribeWhisperV3TurboAsync(
        Uri fileUrl,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(fileUrl.ToString()), "url");

        AddCommonFields(content);

        return await SendAsync(content, cancellationToken);
    }

    private static void AddCommonFields(MultipartFormDataContent content)
    {
        content.Add(new StringContent(Model), "model");
        content.Add(new StringContent("0"), "temperature");
        content.Add(new StringContent("verbose_json"), "response_format");
        content.Add(new StringContent("word"), "timestamp_granularities[]");
    }

    private async Task<GroqVerboseResponse> SendAsync(
        MultipartFormDataContent content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Content = content;

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Groq transcription request failed with status {(int)response.StatusCode}: {errorBody}");
        }

        var payload =
            await response.Content.ReadFromJsonAsync<GroqVerboseResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Groq API returned an empty response.");

        return payload;
    }
}
