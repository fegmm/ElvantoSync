using System.Text.Json.Serialization;
using System.Linq;

namespace Nextcloud.Models.Provisioning;

public record EditUserRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("quota")]
    public string? Quota { get; init; }

    [JsonPropertyName("displayname")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("website")]
    public string? Website { get; init; }

    [JsonPropertyName("twitter")]
    public string? Twitter { get; init; }

    [JsonPropertyName("password")]
    public string? Password { get; init; }

    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();
        typeof(EditUserRequest).GetProperties().ToList().ForEach(p =>
        {
            var attr = p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                        .FirstOrDefault() as JsonPropertyNameAttribute;

            var name = attr?.Name ?? p.Name;
            var value = p.GetValue(this);
            if (value != null) dict.Add(name, value);
        });
        return dict;
    }
}