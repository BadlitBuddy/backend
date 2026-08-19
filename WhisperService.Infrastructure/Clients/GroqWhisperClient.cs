using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Dtos.TranscriptionProviderResults;

namespace Infrastructure.Clients;

public static class GroqWhisperModels
{
    public const string WhisperV3LargeTurbo = "whisper-large-v3-turbo";
    public const string WhisperV3Large = "whisper-large-v3";
    public const string Default = WhisperV3LargeTurbo;
}

public class GroqWhisperClient
{
    private const string Endpoint = "audio/transcriptions";
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    private readonly HttpClient _httpClient;

    public GroqWhisperClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<GroqVerboseResponse> TranscribeAsync(
        FileInfo fileInfo,
        string model = GroqWhisperModels.Default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Audio file not found.", fileInfo.FullName);
        }

        if (fileInfo.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File '{fileInfo.Name}' ({fileInfo.Length} bytes) exceeds Groq's {MaxFileSizeBytes} byte limit.");
        }

        if (fileInfo.Extension != ".flac")
        {
            throw new InvalidOperationException("Only flac files are  supported.");
        }

        var fileName = fileInfo.FullName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name (with extension) is required.", nameof(fileName));
        }

        await using var fileStream = fileInfo.OpenRead();
        return await TranscribeInternalAsync(
            content =>
            {
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/x-flac");
                content.Add(fileContent, "file", fileName);
            },
            model,
            cancellationToken);
    }

    public Task<GroqVerboseResponse> TranscribeAsync(
        Uri uri,
        string model = GroqWhisperModels.Default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return TranscribeInternalAsync(
            content => content.Add(new StringContent(uri.ToString()), "url"),
            model,
            cancellationToken);
    }

    private async Task<GroqVerboseResponse> TranscribeInternalAsync(
        Action<MultipartFormDataContent> addAudioSource,
        string model,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        AddCommonFields(content, model);
        addAudioSource(content);

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Content = content;
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Groq transcription request failed with status {(int)response.StatusCode}: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<GroqVerboseResponse>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Groq API returned an empty response.");
    }

    private static void AddCommonFields(MultipartFormDataContent content, string model)
    {
        content.Add(new StringContent(model), "model");
        content.Add(new StringContent("0"), "temperature");
        content.Add(new StringContent("verbose_json"), "response_format");
        content.Add(new StringContent("segment"), "timestamp_granularities[]");
    }
}
