using Nextcloud.Utils;
using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

internal record GetUserResponse {
    [JsonPropertyName("users")]
    [JsonConverter(typeof(DictionaryOrEmptyArrayConverter<string, User>))]
    public required Dictionary<string, User> Users { get; init; }
} 