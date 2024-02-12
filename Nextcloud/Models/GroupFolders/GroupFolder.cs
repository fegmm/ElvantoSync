using Nextcloud.Utils;
using System.Text.Json.Serialization;

namespace Nextcloud.Models.GroupFolders;

public record GroupFolder
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }


    [JsonPropertyName("groups")]
    [JsonConverter(typeof(DictionaryOrEmptyArrayConverter<string, int>))]
    public required Dictionary<string, int>? Groups { get; init; }


    [JsonPropertyName("mount_point")]
    public required string MountPoint { get; init; }

    [JsonPropertyName("quota")]
    public required long Quota { get; init; }

    [JsonPropertyName("size")]
    public required long Size { get; init; }

    [JsonPropertyName("acl")]
    public required bool Acl { get; init; }

    [JsonPropertyName("manage")]
    [JsonConverter(typeof(DictionaryOrArrayConverter<ACLManage>))]
    public required IEnumerable<ACLManage> Manage { get; init; }

    [JsonPropertyName("group_details")]
    [JsonConverter(typeof(DictionaryOrEmptyArrayConverter<string, GroupDetail>))]
    public required Dictionary<string, GroupDetail> GroupDetails { get; init; }
}

public record ACLManage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("displayname")]
    public required string Displayname { get; init; }
}

public record GroupDetail
{

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("permissions")]
    public required int Permissions { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }
}