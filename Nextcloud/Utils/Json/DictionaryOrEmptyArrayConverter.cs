using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nextcloud.Utils.Json;
public class DictionaryOrEmptyArrayConverter<T, F> : JsonConverter<Dictionary<T, F>> where T : notnull
{
    public override Dictionary<T, F>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jNode = JsonNode.Parse(ref reader);
        return jNode switch
        {
            JsonArray jsonArray when jsonArray.Count == 0 => new Dictionary<T, F>(),
            JsonArray jsonArray => throw new JsonException("Cannot convert non-empty array to dictionary"),
            null => null,
            _ => JsonSerializer.Deserialize<Dictionary<T, F>>(jNode.ToString())
        };
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<T, F> value, JsonSerializerOptions options)
    {
        if (value.Count == 0)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}