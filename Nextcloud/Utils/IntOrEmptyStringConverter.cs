using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nextcloud.Utils
{
    internal class IntOrEmptyStringConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String when reader.GetString() == "" => null,
                JsonTokenType.Number => reader.GetInt32(),
                _ => throw new JsonException(),
            };
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteStringValue("");
            }
            else
            {
                writer.WriteNumberValue((int)value);
            }
        }
    }
}
