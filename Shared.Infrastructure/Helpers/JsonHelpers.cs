using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Shared.Infrastructure.Helpers;

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

public static class JsonTypeInfoResolvers
{
    public static void IgnoreProviderModelId(JsonTypeInfo typeInfo)
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
}
