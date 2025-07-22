using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nextcloud.Utils.Json;

internal class IntOrBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32() != 0,
            JsonTokenType.False => reader.GetBoolean(),
            JsonTokenType.True => reader.GetBoolean(),
            _ => throw new JsonException(),
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value ? 1 : 0);
    }
}
