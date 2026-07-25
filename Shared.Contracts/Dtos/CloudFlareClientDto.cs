using System.Text.Json.Serialization;

namespace Shared.Contracts.Dtos;

public record CloudflareResponse<T>
{
    [JsonPropertyName("result")] public T? Result { get; init; }

    [JsonPropertyName("success")] public bool Success { get; init; }

    [JsonPropertyName("errors")] public List<CloudflareApiError>? Errors { get; init; }

    [JsonPropertyName("messages")] public List<string>? Messages { get; init; }
}

public record CloudflareApiError
{
    [JsonPropertyName("code")] public int Code { get; init; }

    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
}

public record WhisperV3TurboRequest
{
    /// <summary>
    /// Base64 encoded string. Ignored when using the streaming transcription overload
    /// (<see cref="TranscribeWhisperV3TurboAsync(string, WhisperV3TurboRequest, CancellationToken)"/>).
    /// </summary>
    [JsonPropertyName("audio")]
    public string? Audio { get; init; }

    /// <summary>
    /// Supported tasks are 'translate' or 'transcribe'.
    /// </summary>
    [JsonPropertyName("task")]
    public string Task { get; init; } = "transcribe";

    /// <summary>
    /// The language of the audio being transcribed or translated.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; init; }

    /// <summary>
    /// Preprocess the audio with a voice activity detection model.
    /// </summary>
    [JsonPropertyName("vad_filter")]
    public bool VadFilter { get; init; } = false;

    /// <summary>
    /// A text prompt to help provide context to the model on the contents of the audio.
    /// </summary>
    [JsonPropertyName("initial_prompt")]
    public string? InitialPrompt { get; init; }

    /// <summary>
    /// The prefix appended to the beginning of the output of the transcription.
    /// </summary>
    [JsonPropertyName("prefix")]
    public string? Prefix { get; init; }

    /// <summary>
    /// The number of beams to use in beam search decoding.
    /// </summary>
    [JsonPropertyName("beam_size")]
    public int BeamSize { get; init; } = 5;

    /// <summary>
    /// Whether to condition on previous text during transcription.
    /// </summary>
    [JsonPropertyName("condition_on_previous_text")]
    public bool ConditionOnPreviousText { get; init; } = true;

    /// <summary>
    /// Threshold for detecting no-speech segments.
    /// </summary>
    [JsonPropertyName("no_speech_threshold")]
    public double NoSpeechThreshold { get; init; } = 0.6;

    /// <summary>
    /// Threshold for filtering out segments with high compression ratio.
    /// </summary>
    [JsonPropertyName("compression_ratio_threshold")]
    public double CompressionRatioThreshold { get; init; } = 2.4;

    /// <summary>
    /// Threshold for filtering out segments with low average log probability.
    /// </summary>
    [JsonPropertyName("log_prob_threshold")]
    public double LogProbThreshold { get; init; } = -1.0;

    /// <summary>
    /// Optional threshold (in seconds) to skip silent periods.
    /// </summary>
    [JsonPropertyName("hallucination_silence_threshold")]
    public double? HallucinationSilenceThreshold { get; init; }
}
