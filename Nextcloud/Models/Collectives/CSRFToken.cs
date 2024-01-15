using System.Text.Json.Serialization;

namespace Nextcloud.Models.Collectives;

internal record CSRFToken
{
    [JsonPropertyName("token")]
    public required string Token { get; init; }
}
