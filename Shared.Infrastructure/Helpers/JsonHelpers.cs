using System.Text.Json;
using System.Text.Json.Serialization;

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
