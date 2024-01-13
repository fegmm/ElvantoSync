using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

public record EditUserRequest(
    string? Email = null,
    string? Quota = null,
    [property: JsonPropertyName("displayname")] string? DisplayName = null,
    string? Phone = null,
    string? Address = null,
    string? Website = null,
    string? Twitter = null,
    string? Password = null
)
{
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();
        typeof(EditUserRequest).GetProperties().ToList().ForEach(p =>
        {
            var value = p.GetValue(this);
            if (value != null) dict.Add(p.Name.ToLower(), value);
        });
        return dict;
    }
}