using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaDental.API.Converters
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return TimeSpan.Parse(reader.GetString()!);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                long? ticks = null;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString()?.Equals("ticks", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        reader.Read();
                        ticks = reader.GetInt64();
                    }
                }

                if (ticks.HasValue)
                {
                    return new TimeSpan(ticks.Value);
                }
            }

            throw new JsonException("No se pudo convertir el valor a TimeSpan.");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}