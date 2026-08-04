using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;
using Shared.Infrastructure.Helpers;

namespace Shared.Infrastructure.Services;

public class StreamableTranscriptionExporter : IStreamableTranscriptionExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TimeSpanToSecondsJsonConverter()
        },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { IgnoreProviderModelId }
        }
    };

    private static void IgnoreProviderModelId(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(TranscriptionResult)) return;

        var property = typeInfo.Properties.FirstOrDefault(p => p.Name == nameof(TranscriptionResult.ProviderModelId));
        if (property is not null)
        {
            typeInfo.Properties.Remove(property);
        }
    }

    public async Task ExportAsync(TranscriptionResult result, TranscriptionExportFormat format, Stream destination,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (format == TranscriptionExportFormat.Json)
        {
            await ToJsonAsync(result, destination, ct).ConfigureAwait(false);
            return;
        }

        await using var writer = new StreamWriter(destination, Encoding.UTF8, bufferSize: 8192, leaveOpen: true);

        switch (format)
        {
            case TranscriptionExportFormat.Srt:
                await ToSrtAsync(result, writer, ct).ConfigureAwait(false);
                break;
            case TranscriptionExportFormat.Vtt:
                await ToVttAsync(result, writer, ct).ConfigureAwait(false);
                break;
            case TranscriptionExportFormat.Txt:
                await ToTxtAsync(result, writer, ct).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }

        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    public async Task ToSrtAsync(TranscriptionResult result, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        if (result.Segments.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                var end = result.Duration ?? TimeSpan.Zero;
                await writer.WriteLineAsync("1");
                await WriteTimestampLineAsync(writer, TimeSpan.Zero, end, ',', ct);
                await writer.WriteLineAsync(result.Text.AsMemory().Trim());
                await writer.WriteLineAsync();
            }

            return;
        }

        var index = 1;
        foreach (var segment in result.Segments)
        {
            ct.ThrowIfCancellationRequested();

            await writer.WriteLineAsync(index.ToString(CultureInfo.InvariantCulture));
            await WriteTimestampLineAsync(writer, segment.Start, segment.End, ',', ct);
            await writer.WriteLineAsync(segment.Text.AsMemory().Trim());
            await writer.WriteLineAsync();
            index++;
        }
    }

    public async Task ToVttAsync(TranscriptionResult result, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        if (!string.IsNullOrWhiteSpace(result.Vtt))
        {
            await writer.WriteAsync(result.Vtt);
            return;
        }

        await writer.WriteLineAsync("WEBVTT");
        await writer.WriteLineAsync();

        if (result.Segments.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                var end = result.Duration ?? TimeSpan.Zero;
                await WriteTimestampLineAsync(writer, TimeSpan.Zero, end, '.', ct);
                await writer.WriteLineAsync(result.Text.AsMemory().Trim());
                await writer.WriteLineAsync();
            }

            return;
        }

        foreach (var segment in result.Segments)
        {
            ct.ThrowIfCancellationRequested();

            await WriteTimestampLineAsync(writer, segment.Start, segment.End, '.', ct);
            await writer.WriteLineAsync(segment.Text.AsMemory().Trim());
            await writer.WriteLineAsync();
        }
    }

    public async Task ToTxtAsync(TranscriptionResult result, TextWriter writer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        if (result.Segments.Count == 0)
        {
            await writer.WriteAsync(result.Text.AsMemory().Trim());
            return;
        }

        for (var i = 0; i < result.Segments.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var text = result.Segments[i].Text.AsMemory().Trim();

            if (i == result.Segments.Count - 1)
            {
                await writer.WriteAsync(text);
            }
            else
            {
                await writer.WriteLineAsync(text);
            }
        }
    }

    public Task ToJsonAsync(TranscriptionResult result, Stream destination, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(destination);

        // JsonSerializer.SerializeAsync writes directly to the stream in chunks without intermediate string allocations
        return JsonSerializer.SerializeAsync(destination, result, JsonOptions, ct);
    }

    // --- High Performance String Formatters (Zero-Allocation Spans) ---

    private static async Task WriteTimestampLineAsync(TextWriter writer, TimeSpan start, TimeSpan end, char separator,
        CancellationToken ct)
    {
        // Allocate a small 30-character buffer on the stack (0 heap allocation!)
        Span<char> buffer = stackalloc char[30];

        FormatTimestampSpan(start, separator, buffer[..12]);
        buffer[12] = ' ';
        buffer[13] = '-';
        buffer[14] = '>';
        buffer[15] = ' ';
        FormatTimestampSpan(end, separator, buffer[16..28]);

        await writer.WriteLineAsync(buffer[..28].ToString()); // Or write memory directly if custom TextWriter
    }

    private static void FormatTimestampSpan(TimeSpan time, char msSeparator, Span<char> destination)
    {
        if (time < TimeSpan.Zero) time = TimeSpan.Zero;

        // Formats directly into the provided span (HH:MM:SS,mmm)
        _ = ((int)time.TotalHours).TryFormat(destination[..2], out _, "D2", CultureInfo.InvariantCulture);
        destination[2] = ':';
        _ = time.Minutes.TryFormat(destination[3..5], out _, "D2", CultureInfo.InvariantCulture);
        destination[5] = ':';
        _ = time.Seconds.TryFormat(destination[6..8], out _, "D2", CultureInfo.InvariantCulture);
        destination[8] = msSeparator;
        _ = time.Milliseconds.TryFormat(destination[9..12], out _, "D3", CultureInfo.InvariantCulture);
    }
}
