using System.Text.Json.Serialization;

namespace Nextcloud.Models;

internal record IdResponse<T>
{
    [JsonPropertyName("id")]
    public required T Id { get; init; }
}

internal record IdResponse : IdResponse<string>
{
}   