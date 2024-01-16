using Nextcloud.Utils;
using System.Text.Json.Serialization;

namespace Nextcloud.Models.GroupFolders;

internal record GetGroupFolderOcsResponse
{
    [JsonConverter(typeof(DictionaryOrEmptyArrayConverter<string, GroupFolder>))]
    [JsonPropertyName("data")]
    public required Dictionary<string, GroupFolder> Data { get; init; }

    [JsonPropertyName("meta")]
    public required OCSMeta Meta { get; init; }
}

internal record GetGroupFolderResponse
{
    [JsonPropertyName("ocs")]
    public required GetGroupFolderOcsResponse Ocs { get; init; }
}