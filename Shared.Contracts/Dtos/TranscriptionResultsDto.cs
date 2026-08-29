using System.ComponentModel;
using Shared.Contracts.Enums;

namespace Shared.Contracts.Dtos;

/// <summary>Transcription task requested from the provider.</summary>
public enum TranscriptionTask
{
    [Description("Transcribe")] Transcribe,
    [Description("Translate")] Translate
}

/// <summary>
/// Provider-agnostic transcription result. Returned by every
/// <see cref="ITranscriptionProvider"/> implementation.
/// </summary>
public sealed record TranscriptionResult
{
    /// <summary>Full concatenated transcription text. Always present.</summary>
    public required string Text { get; init; }

    /// <summary>Detected or forced language code (e.g. "en", "ja").</summary>
    public string? Language { get; init; }

    /// <summary>Confidence of detected language (0–1). Cloudflare only.</summary>
    public double? LanguageProbability { get; init; }

    /// <summary>Total audio duration.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Audio duration after VAD filtering (Cloudflare).</summary>
    public TimeSpan? DurationAfterVad { get; init; }

    /// <summary>Task performed by the model.</summary>
    public TranscriptionTask Task { get; init; } = TranscriptionTask.Transcribe;

    /// <summary>Segment-level results with timestamps and confidence metrics.</summary>
    public IReadOnlyList<TranscriptionSegment> Segments { get; init; } = [];

    /// <summary>Top-level word timestamps (Groq/OpenAI when timestamp_granularities=word).</summary>
    public IReadOnlyList<TranscriptionWord> Words { get; init; } = [];

    /// <summary>Total word count, when the provider returns it (Cloudflare).</summary>
    public int? WordCount { get; init; }

    /// <summary>WebVTT-formatted transcript blob (Cloudflare).</summary>
    public string? Vtt { get; init; }

    /// <summary>Identifier of the model that produced this result.</summary>
    public TranscriptionProvider? ProviderModelId { get; init; }
}

/// <summary>
/// One transcribed segment with all confidence metrics that any of the
/// supported providers may emit. All metrics are nullable so a provider
/// that does not populate them simply leaves them null.
/// </summary>
public sealed record TranscriptionSegment
{
    // --- Identity (Groq/OpenAI only) ---
    public int? Id { get; init; }
    public int? Seek { get; init; }

    // --- Core content (always present) ---
    public required string Text { get; init; }
    public required TimeSpan Start { get; init; }
    public required TimeSpan End { get; init; }

    /// <summary>Per-segment language (Whisper.net emits this on each segment).</summary>
    public string? Language { get; init; }

    // --- Confidence / quality metrics ---
    // Whisper.net
    public float? Probability { get; init; }
    public float? MinProbability { get; init; }
    public float? MaxProbability { get; init; }

    // Whisper.net + Groq + Cloudflare
    public float? NoSpeechProbability { get; init; }

    // Groq + Cloudflare
    public double? AverageLogProbability { get; init; }
    public double? CompressionRatio { get; init; }
    public double? Temperature { get; init; }

    // Groq/OpenAI only
    public bool? IsTransient { get; init; }

    // --- Token / word breakdown ---
    /// <summary>
    /// Whisper tokens. For Whisper.net these carry full per-token metadata;
    /// for Groq/OpenAI these are the bare vocabulary ids from <c>tokens[]</c>;
    /// for Cloudflare this is empty (Cloudflare exposes words, not tokens).
    /// </summary>
    public IReadOnlyList<TranscriptionToken> Tokens { get; init; } = [];

    /// <summary>Inline word-level timestamps (Cloudflare returns these per segment).</summary>
    public IReadOnlyList<TranscriptionWord> Words { get; init; } = [];
}

/// <summary>A single transcribed word with timing.</summary>
public sealed record TranscriptionWord
{
    public required string Word { get; init; }
    public required TimeSpan Start { get; init; }
    public required TimeSpan End { get; init; }

    /// <summary>Word-level probability (Groq/OpenAI only).</summary>
    public double? Probability { get; init; }
}

/// <summary>
/// A Whisper token. The common surface is <see cref="Id"/> and (optionally)
/// <see cref="Text"/>; the remaining fields are Whisper.net-specific and will
/// be null for Groq/Cloudflare.
/// </summary>
public sealed record TranscriptionToken
{
    /// <summary>Whisper vocabulary token id. Always present when tokens are returned.</summary>
    public required int Id { get; init; }

    /// <summary>Decoded text fragment for this token, when available.</summary>
    public string? Text { get; init; }

    public TimeSpan? Start { get; init; }
    public TimeSpan? End { get; init; }

    // --- Whisper.net-only detailed metadata ---
    public int? TimestampId { get; init; }
    public float? Probability { get; init; }
    public double? LogProbability { get; init; } // WhisperToken.ProbabilityLog
    public float? TimestampProbability { get; init; }
    public float? TimestampProbabilitySum { get; init; }
    public long? DtwTimestamp { get; init; } // DTW-aligned timestamp (ms)
    public float? VoiceLength { get; init; }
}

public sealed record SrtSegment
{
    public int Index { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed record VttSegment
{
    public int Index { get; set; }
    public required string StartTime { get; set; }
    public required string EndTime { get; set; }
    public required string Text { get; set; }
    public string? Voice { get; set; }
}
