using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Shared.Abstractions.Services;
using Shared.Contracts.Enums;

namespace Shared.Infrastructure.Services;

public class TranscriptionExporter : ITranscriptionExporter
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
        if (typeInfo.Type != typeof(TranscriptionResult))
        {
            return;
        }

        var property = typeInfo.Properties.FirstOrDefault(p => p.Name == nameof(TranscriptionResult.ProviderModelId));
        if (property is not null)
        {
            typeInfo.Properties.Remove(property);
        }
    }

    public string Export(TranscriptionResult result, TranscriptionExportFormat format) => format switch
    {
        TranscriptionExportFormat.Srt => ToSrt(result),
        TranscriptionExportFormat.Vtt => ToVtt(result),
        TranscriptionExportFormat.Txt => ToTxt(result),
        TranscriptionExportFormat.Json => ToJson(result),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    public string ToSrt(TranscriptionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Segments.Count == 0)
        {
            var end = result.Duration ?? TimeSpan.Zero;
            return string.IsNullOrWhiteSpace(result.Text)
                ? string.Empty
                : $"1{Environment.NewLine}" +
                  $"{FormatSrtTimestamp(TimeSpan.Zero)} --> {FormatSrtTimestamp(end)}{Environment.NewLine}" +
                  $"{result.Text.Trim()}{Environment.NewLine}{Environment.NewLine}";
        }

        var sb = new StringBuilder();
        var index = 1;

        foreach (var segment in result.Segments)
        {
            sb.Append(index).Append(Environment.NewLine);
            sb.Append(FormatSrtTimestamp(segment.Start))
                .Append(" --> ")
                .Append(FormatSrtTimestamp(segment.End))
                .Append(Environment.NewLine);
            sb.Append(segment.Text.Trim()).Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            index++;
        }

        return sb.ToString();
    }

    public string ToVtt(TranscriptionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Prefer the provider's own VTT blob when it supplied one (e.g. Cloudflare).
        if (!string.IsNullOrWhiteSpace(result.Vtt))
        {
            return result.Vtt;
        }

        var sb = new StringBuilder();
        sb.Append("WEBVTT").Append(Environment.NewLine).Append(Environment.NewLine);

        if (result.Segments.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                var end = result.Duration ?? TimeSpan.Zero;
                sb.Append(FormatVttTimestamp(TimeSpan.Zero))
                    .Append(" --> ")
                    .Append(FormatVttTimestamp(end))
                    .Append(Environment.NewLine);
                sb.Append(result.Text.Trim()).Append(Environment.NewLine).Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        foreach (var segment in result.Segments)
        {
            sb.Append(FormatVttTimestamp(segment.Start))
                .Append(" --> ")
                .Append(FormatVttTimestamp(segment.End))
                .Append(Environment.NewLine);
            sb.Append(segment.Text.Trim()).Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
        }

        return sb.ToString();
    }

    public string ToTxt(TranscriptionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Segments.Count == 0)
        {
            return result.Text.Trim();
        }

        var sb = new StringBuilder();
        foreach (var segment in result.Segments)
        {
            sb.AppendLine(segment.Text.Trim());
        }

        return sb.ToString().TrimEnd();
    }

    public string ToJson(TranscriptionResult result)
    {
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    // SRT uses a comma as the millisecond separator: HH:MM:SS,mmm
    private static string FormatSrtTimestamp(TimeSpan time) =>
        FormatTimestamp(time, ',');

    // VTT uses a period as the millisecond separator: HH:MM:SS.mmm
    private static string FormatVttTimestamp(TimeSpan time) =>
        FormatTimestamp(time, '.');

    private static string FormatTimestamp(TimeSpan time, char msSeparator)
    {
        if (time < TimeSpan.Zero)
        {
            time = TimeSpan.Zero;
        }

        return string.Create(CultureInfo.InvariantCulture,
            $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}{msSeparator}{time.Milliseconds:D3}");
    }
}

public class TimeSpanToSecondsJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return TimeSpan.FromSeconds(reader.GetDouble());
        }

        if (reader.TokenType == JsonTokenType.String && TimeSpan.TryParse(reader.GetString(), out var parsed))
        {
            return parsed;
        }

        return TimeSpan.Zero;
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(Math.Round(value.TotalSeconds, 3));
    }
}
