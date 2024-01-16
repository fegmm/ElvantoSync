using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

internal record GetGroupResponse {
    [JsonPropertyName("groups")]
    public required IEnumerable<Group> Groups { get; init; }
}