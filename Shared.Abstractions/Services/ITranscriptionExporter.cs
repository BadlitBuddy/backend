using Shared.Contracts.Enums;

namespace Shared.Abstractions.Services;

public interface IStreamableTranscriptionExporter
{
    /// <summary>
    /// Asynchronously exports the transcription result to a destination stream using the specified export format.
    /// </summary>
    Task ExportAsync(TranscriptionResult result, TranscriptionExportFormat format, Stream destination,
        CancellationToken ct = default);

    /// <summary>
    /// Asynchronously writes the transcription result in SubRip (.srt) subtitle format directly to a <see cref="TextWriter"/>.
    /// </summary>
    Task ToSrtAsync(TranscriptionResult result, TextWriter writer, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously writes the transcription result in WebVTT (.vtt) format directly to a <see cref="TextWriter"/>.
    /// Uses provider-supplied VTT data when present; otherwise, constructs it from segment timings.
    /// </summary>
    Task ToVttAsync(TranscriptionResult result, TextWriter writer, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously writes the transcription result as plain text directly to a <see cref="TextWriter"/>.
    /// Formats as one line per segment if segments exist, or raw text otherwise.
    /// </summary>
    Task ToTxtAsync(TranscriptionResult result, TextWriter writer, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously writes the transcription result as indented JSON directly to a destination stream, 
    /// including all provider-specific metadata.
    /// </summary>
    Task ToJsonAsync(TranscriptionResult result, Stream destination, CancellationToken ct = default);
}

public interface ITranscriptionExporter
{
    /// <summary>Export to SubRip (.srt) subtitle format, built from segment timings.</summary>
    string ToSrt(TranscriptionResult result);

    /// <summary>Export to WebVTT (.vtt) format. Uses the provider-supplied VTT blob when present
    /// (e.g. Cloudflare), otherwise builds it from segment timings.</summary>
    string ToVtt(TranscriptionResult result);

    /// <summary>Export to plain text. One line per segment if segments exist, otherwise the raw text.</summary>
    string ToTxt(TranscriptionResult result);

    /// <summary>Export to indented JSON, including all provider-specific metadata.</summary>
    string ToJson(TranscriptionResult result);

    /// <summary>Convenience dispatcher over <see cref="TranscriptionExportFormat"/>.</summary>
    string Export(TranscriptionResult result, TranscriptionExportFormat format);
}
