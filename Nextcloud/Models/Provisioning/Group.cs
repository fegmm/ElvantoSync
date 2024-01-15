using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

public record Group
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }
    
    [JsonPropertyName("userCount")]
    public required int UserCount { get; init; }
    
    [JsonPropertyName("disabled")]
    public required int Disabled { get; init; }
    
    [JsonPropertyName("canAdd")]
    public required bool CanAdd { get; init; }
    
    [JsonPropertyName("canRemove")]
    public required bool CanRemove { get; init; }
}
