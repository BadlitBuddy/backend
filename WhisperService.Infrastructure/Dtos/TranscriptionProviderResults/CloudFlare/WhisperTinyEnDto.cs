using System.Text.Json.Serialization;
using Shared.Contracts.Dtos;
using Shared.Contracts.Enums;

namespace Infrastructure.Dtos.TranscriptionProviderResults.CloudFlare;

public class CfWhisperTinyEnResponse
{
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;

    [JsonPropertyName("word_count")] public int WordCount { get; set; }

    [JsonPropertyName("vtt")] public string Vtt { get; set; } = string.Empty;

    [JsonPropertyName("words")] public List<CfWhisperTinyEnTranscriptionWord> Words { get; set; } = new();

    [JsonPropertyName("segments")] public List<CfWhisperTinyEnTranscriptionSegment> Segments { get; set; } = new();

    [JsonPropertyName("usage")] public CfWhisperTinyEnTranscriptionUsage Usage { get; set; } = new();
}

public class CfWhisperTinyEnTranscriptionWord
{
    [JsonPropertyName("word")] public string Word { get; set; } = string.Empty;

    [JsonPropertyName("start")] public double Start { get; set; }

    [JsonPropertyName("end")] public double End { get; set; }
}

public class CfWhisperTinyEnTranscriptionSegment
{
    [JsonPropertyName("start")] public double Start { get; set; }

    [JsonPropertyName("end")] public double End { get; set; }

    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;

    [JsonPropertyName("temperature")] public double? Temperature { get; set; }

    [JsonPropertyName("avg_logprob")] public double? AvgLogprob { get; set; }

    [JsonPropertyName("compression_ratio")]
    public double? CompressionRatio { get; set; }

    [JsonPropertyName("no_speech_prob")] public float? NoSpeechProb { get; set; }

    [JsonPropertyName("words")] public List<CfWhisperTinyEnTranscriptionWord> Words { get; set; } = new();

    [JsonPropertyName("word_count")] public int? WordCount { get; set; }
}

public class CfWhisperTinyEnTranscriptionUsage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }

    [JsonPropertyName("prompt_tokens_details")]
    public CfWhisperTinyEnPromptTokensDetails PromptTokensDetails { get; set; } = new();

    [JsonPropertyName("neurons")] public double Neurons { get; set; }
}

public class CfWhisperTinyEnPromptTokensDetails
{
    [JsonPropertyName("cached_tokens")] public int CachedTokens { get; set; }
}

public static class CfWhisperTinyEnResponseMapper
{
    public static TranscriptionResult ToResult(CfWhisperTinyEnResponse r)
    {
        return new TranscriptionResult
        {
            Text = r.Text,
            WordCount = r.WordCount,
            Vtt = r.Vtt,
            Words = r.Words.Select(ToWord).ToArray(),
            Segments = r.Segments.Select(ToSegment).ToArray(),
            ProviderModelId = TranscriptionProvider.Cloudflare
        };
    }

    private static TranscriptionSegment ToSegment(CfWhisperTinyEnTranscriptionSegment s) => new()
    {
        Text = s.Text,
        Start = TimeSpan.FromSeconds(s.Start),
        End = TimeSpan.FromSeconds(s.End),
        NoSpeechProbability = s.NoSpeechProb,
        AverageLogProbability = s.AvgLogprob,
        CompressionRatio = s.CompressionRatio,
        Temperature = s.Temperature,
        Words = s.Words.Select(ToWord).ToArray()
    };

    private static TranscriptionWord ToWord(CfWhisperTinyEnTranscriptionWord w) => new()
    {
        Word = w.Word,
        Start = TimeSpan.FromSeconds(w.Start),
        End = TimeSpan.FromSeconds(w.End)
    };
}
