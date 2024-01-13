using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

public record Group
(
    string Id,
    [property: JsonPropertyName("displayname")] string DisplayName,
    [property: JsonPropertyName("usercount")] int UserCount,
    int Disabled,
    bool CanAdd,
    bool CanRemove
);