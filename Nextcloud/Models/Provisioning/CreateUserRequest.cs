using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

public record CreateUserRequest(
    [property: JsonPropertyName("userid")] string UserId,
    string? DisplayName,
    string? Email,
    IEnumerable<string>? Groups = null,
    IEnumerable<string>? Subadmin = null,
    string? Manager = null,
    string? Language = null,
    string? Password = null,
    string? Quota = null
);