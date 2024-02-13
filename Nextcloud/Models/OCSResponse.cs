using Nextcloud.Utils;
using System.Text.Json.Serialization;

namespace Nextcloud.Models;

internal record OCSResponse<T>
{
    [JsonPropertyName("ocs")]
    public required OCS<T> Ocs { get; init; }
}

internal record OCS<T>
{
    [JsonPropertyName("data")]
    public required T Data { get; init; }

    [JsonPropertyName("meta")]
    public OCSMeta? Meta { get; init; }
}

internal record OCSMeta
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("statuscode")]
    public required int Statuscode { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
    
    [JsonPropertyName("totalitems")]
    [JsonConverter(typeof(IntOrEmptyStringConverter))] // Groupfolders returns empty string instead of null
    public int? TotalItems { get; init; }

    [JsonPropertyName("itemsperpage")]
    [JsonConverter(typeof(IntOrEmptyStringConverter))] // Groupfolders returns empty string instead of null
    public int? ItemsPerPage { get; init; }
}
