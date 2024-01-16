using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

internal record GetMembersResponse {
    [JsonPropertyName("users")]
    public required IEnumerable<string> Users { get; init; }
}
