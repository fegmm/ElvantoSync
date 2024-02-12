using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nextcloud.Utils
{
    internal class DictionaryOrArrayConverter<F> : JsonConverter<IEnumerable<F>>
    {
        public override IEnumerable<F>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jNode = JsonNode.Parse(ref reader);
            return jNode switch
            {
                JsonArray jsonArray => JsonSerializer.Deserialize<F[]>(jNode.ToString()),
                null => null,
                _ => JsonSerializer.Deserialize<Dictionary<string, F>>(jNode.ToString())!.Select(i => i.Value)
            };
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<F> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}