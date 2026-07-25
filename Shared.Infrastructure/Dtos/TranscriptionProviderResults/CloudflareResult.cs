using System.Text.Json.Serialization;

namespace Shared.Infrastructure.Dtos.TranscriptionProviderResults;

public sealed record CfWhisperResponse
{
    [JsonPropertyName("text")] public string Text { get; init; } = "";
    [JsonPropertyName("word_count")] public int? WordCount { get; init; }
    [JsonPropertyName("vtt")] public string? Vtt { get; init; }
    [JsonPropertyName("segments")] public List<CfSegment>? Segments { get; init; }
    [JsonPropertyName("words")] public List<CfWord>? Words { get; init; } // legacy @cf/openai/whisper shape

    [JsonPropertyName("transcription_info")]
    public CfTranscriptionInfo? Info { get; init; }
}

public sealed record CfTranscriptionInfo
{
    [JsonPropertyName("language")] public string? Language { get; init; }

    [JsonPropertyName("language_probability")]
    public double? LanguageProbability { get; init; }

    [JsonPropertyName("duration")] public double? Duration { get; init; }

    [JsonPropertyName("duration_after_vad")]
    public double? DurationAfterVad { get; init; }
}

public sealed record CfSegment
{
    [JsonPropertyName("start")] public double Start { get; init; }
    [JsonPropertyName("end")] public double End { get; init; }
    [JsonPropertyName("text")] public string Text { get; init; } = "";
    [JsonPropertyName("temperature")] public double Temperature { get; init; }
    [JsonPropertyName("avg_logprob")] public double AvgLogprob { get; init; }

    [JsonPropertyName("compression_ratio")]
    public double CompressionRatio { get; init; }

    [JsonPropertyName("no_speech_prob")] public float NoSpeechProb { get; init; }
    [JsonPropertyName("words")] public List<CfWord>? Words { get; init; }
}

public sealed record CfWord
{
    [JsonPropertyName("word")] public string Word { get; init; } = "";
    [JsonPropertyName("start")] public double Start { get; init; }
    [JsonPropertyName("end")] public double End { get; init; }
}

public static class CloudflareMapper
{
    public static TranscriptionResult ToResult(CfWhisperResponse r)
    {
        return new TranscriptionResult
        {
            Text = r.Text,
            Language = r.Info?.Language,
            LanguageProbability = r.Info?.LanguageProbability,
            Duration = r.Info?.Duration is > 0 ? TimeSpan.FromSeconds(r.Info.Duration.Value) : null,
            DurationAfterVad = r.Info?.DurationAfterVad is > 0
                ? TimeSpan.FromSeconds(r.Info.DurationAfterVad.Value)
                : null,
            WordCount = r.WordCount,
            Vtt = r.Vtt,
            Segments = (r.Segments ?? []).Select(ToSegment).ToArray(),
            Words = (r.Words ?? []).Select(ToWord).ToArray(),
            ProviderModelId = TranscriptionProvider.Cloudflare
        };
    }

    private static TranscriptionSegment ToSegment(CfSegment s) => new()
    {
        Text = s.Text,
        Start = TimeSpan.FromSeconds(s.Start),
        End = TimeSpan.FromSeconds(s.End),
        NoSpeechProbability = s.NoSpeechProb,
        AverageLogProbability = s.AvgLogprob,
        CompressionRatio = s.CompressionRatio,
        Temperature = s.Temperature,
        Words = (s.Words ?? []).Select(ToWord).ToArray()
    };

    private static TranscriptionWord ToWord(CfWord w) => new()
    {
        Word = w.Word,
        Start = TimeSpan.FromSeconds(w.Start),
        End = TimeSpan.FromSeconds(w.End)
    };
}
