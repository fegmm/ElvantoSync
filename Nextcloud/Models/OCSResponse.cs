using System.Text.Json.Serialization;

namespace Nextcloud.Models;

internal record OCSResponse<T>(OCS<T> Ocs);
internal record OCS<T>(T Data, OCSMeta Meta);
internal record OCSMeta
(
    string Status,
    int Statuscode,
    string Message,
    [property: JsonPropertyName("totalitems")] string TotalItems,
    [property: JsonPropertyName("itemsperpage")] string ItemsPerPage
);