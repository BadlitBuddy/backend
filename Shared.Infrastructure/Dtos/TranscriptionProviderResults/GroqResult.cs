using System.Text.Json.Serialization;

namespace Shared.Infrastructure.Dtos.TranscriptionProviderResults;

public sealed record GroqVerboseResponse
{
    [JsonPropertyName("task")] public string? Task { get; init; }
    [JsonPropertyName("language")] public string? Language { get; init; }
    [JsonPropertyName("duration")] public double? Duration { get; init; }
    [JsonPropertyName("text")] public string? Text { get; init; }
    [JsonPropertyName("segments")] public List<GroqSegment>? Segments { get; init; }
    [JsonPropertyName("words")] public List<GroqWord>? Words { get; init; }
}

public sealed record GroqSegment
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("seek")] public int Seek { get; init; }
    [JsonPropertyName("start")] public double Start { get; init; }
    [JsonPropertyName("end")] public double End { get; init; }
    [JsonPropertyName("text")] public string Text { get; init; } = "";
    [JsonPropertyName("tokens")] public int[]? Tokens { get; init; }
    [JsonPropertyName("temperature")] public double Temperature { get; init; }
    [JsonPropertyName("avg_logprob")] public double AvgLogprob { get; init; }

    [JsonPropertyName("compression_ratio")]
    public double CompressionRatio { get; init; }

    [JsonPropertyName("no_speech_prob")] public float NoSpeechProb { get; init; }
    [JsonPropertyName("transient")] public bool Transient { get; init; }
}

public sealed record GroqWord
{
    [JsonPropertyName("word")] public string Word { get; init; } = "";
    [JsonPropertyName("start")] public double Start { get; init; }
    [JsonPropertyName("end")] public double End { get; init; }
    [JsonPropertyName("probability")] public double Probability { get; init; }
}

public static class GroqMapper
{
    public static TranscriptionResult ToResult(GroqVerboseResponse r)
    {
        var task = string.Equals(r.Task, "translate", StringComparison.OrdinalIgnoreCase)
            ? TranscriptionTask.Translate
            : TranscriptionTask.Transcribe;

        return new TranscriptionResult
        {
            Text = r.Text ?? "",
            Language = r.Language,
            Duration = r.Duration is > 0 ? TimeSpan.FromSeconds(r.Duration.Value) : null,
            Task = task,
            Segments = (r.Segments ?? []).Select(ToSegment).ToArray(),
            Words = (r.Words ?? []).Select(ToWord).ToArray(),
            ProviderModelId = TranscriptionProvider.Groq
        };
    }

    private static TranscriptionSegment ToSegment(GroqSegment s) => new()
    {
        Id = s.Id,
        Seek = s.Seek,
        Text = s.Text,
        Start = TimeSpan.FromSeconds(s.Start),
        End = TimeSpan.FromSeconds(s.End),
        NoSpeechProbability = s.NoSpeechProb,
        AverageLogProbability = s.AvgLogprob,
        CompressionRatio = s.CompressionRatio,
        Temperature = s.Temperature,
        IsTransient = s.Transient,
        Tokens = (s.Tokens ?? []).Select(id => new TranscriptionToken { Id = id }).ToArray()
    };

    private static TranscriptionWord ToWord(GroqWord w) => new()
    {
        Word = w.Word,
        Start = TimeSpan.FromSeconds(w.Start),
        End = TimeSpan.FromSeconds(w.End),
        Probability = w.Probability
    };
}
