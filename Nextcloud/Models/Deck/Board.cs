using Nextcloud.Utils;
using System.Text.Json.Serialization;

namespace Nextcloud.Models.Deck;

public record Board
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    
    [JsonPropertyName("owner")]
    public required object Owner { get; init; }
    
    [JsonPropertyName("color")]
    public required string Color { get; init; }
    
    [JsonPropertyName("archived")]
    public required bool Archived { get; init; }
    
    [JsonPropertyName("labels")]
    public required object Labels { get; init; }
    
    [JsonPropertyName("acl")]
    public required object Acl { get; init; }
    
    [JsonPropertyName("permissions")]
    public required object Permissions { get; init; }
    
    [JsonPropertyName("users")]
    public required object Users { get; init; }
    
    [JsonPropertyName("shared")]
    [JsonConverter(typeof(IntToBooleanConverter))]
    public required bool Shared { get; init; }
    
    [JsonPropertyName("deletedAt")]
    public required long DeletedAt { get; init; }
    
    [JsonPropertyName("id")]
    public required int Id { get; init; }
    
    [JsonPropertyName("lastModified")]
    public required long LastModified { get; init; }
    
    [JsonPropertyName("settings")]
    public required object Settings { get; init; }
}
