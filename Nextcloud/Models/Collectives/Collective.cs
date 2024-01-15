using Nextcloud.Models.Circles;
using System.Text.Json.Serialization;

namespace Nextcloud.Models.Collectives;

public record Collective
{

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("circleId")]
    public required string CircleId { get; init; }

    [JsonPropertyName("emoji")]
    public required string Emoji { get; init; }

    [JsonPropertyName("trashTimestamp")]
    public required object TrashTimestamp { get; init; }

    [JsonPropertyName("pageMode")]
    public required int PageMode { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("level")]
    public required MemberLevels Level { get; init; }

    [JsonPropertyName("editPermissionLevel")]
    public required int EditPermissionLevel { get; init; }

    [JsonPropertyName("sharePermissionLevel")]
    public required int SharePermissionLevel { get; init; }

    [JsonPropertyName("canEdit")]
    public required bool CanEdit { get; init; }

    [JsonPropertyName("canShare")]
    public required bool CanShare { get; init; }

    [JsonPropertyName("shareToken")]
    public required object ShareToken { get; init; }

    [JsonPropertyName("shareEditable")]
    public required bool ShareEditable { get; init; }

    [JsonPropertyName("userPageOrder")]
    public required int UserPageOrder { get; init; }

    [JsonPropertyName("userShowRecentPage")]
    public required bool UserShowRecentPage { get; init; }
}