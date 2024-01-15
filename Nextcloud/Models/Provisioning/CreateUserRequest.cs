using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

public record CreateUserRequest
{
    [JsonPropertyName("userid")]
    public required string UserId { get; init; }

    [JsonPropertyName("displayname")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("groups")]
    public IEnumerable<string>? Groups { get; init; }

    [JsonPropertyName("subadmin")]
    public IEnumerable<string>? Subadmin { get; init; }

    [JsonPropertyName("manager")]
    public string? Manager { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("password")]
    public string? Password { get; init; }
    
    [JsonPropertyName("quota")]
    public string? Quota { get; init; }
}