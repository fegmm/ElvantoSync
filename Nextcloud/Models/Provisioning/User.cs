using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

public record User
{
    [JsonPropertyName("additional_mail")]
    public required string[] AdditionalMail { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("backend")]
    public required string Backend { get; init; }

    [JsonPropertyName("backendCapabilities")]
    public required Backendcapabilities BackendCapabilities { get; init; }

    [JsonPropertyName("biography")]
    public required string Biography { get; init; }

    [JsonPropertyName("displayname")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    [JsonPropertyName("fediverse")]
    public required string Fediverse { get; init; }

    [JsonPropertyName("groups")]
    public required string[] Groups { get; init; }

    [JsonPropertyName("headline")]
    public required string Headline { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("language")]
    public required string Language { get; init; }

    [JsonPropertyName("lastLogin")]
    public required long LastLogin { get; init; }

    [JsonPropertyName("locale")]
    public required string Locale { get; init; }

    [JsonPropertyName("manager")]
    public required string Manager { get; init; }

    [JsonPropertyName("notify_email ")]
    public string NotifyEmail { get; init; }

    [JsonPropertyName("organisation")]
    public required string Organisation { get; init; }

    [JsonPropertyName("phone")]
    public required string Phone { get; init; }

    [JsonPropertyName("profile_enabled")]
    public required string ProfileEnabled { get; init; }

    [JsonPropertyName("quota")]
    public required UserQuota Quota { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("storageLocation")]
    public required string StorageLocation { get; init; }

    [JsonPropertyName("subadmin")]
    public required string[] Subadmin { get; init; }

    [JsonPropertyName("twitter")]
    public required string Twitter { get; init; }

    [JsonPropertyName("website")]
    public required string Website { get; init; }
}

public record Backendcapabilities
{
    [JsonPropertyName("setDisplayName")]
    public required bool SetDisplayName { get; init; }

    [JsonPropertyName("setPassword")]
    public required bool SetPassword { get; init; }
}

public record UserQuota
{
    [JsonPropertyName("free")]
    public long? Free { get; init; }
    [JsonPropertyName("quota")]
    public required long Quota {get; init;}
    [JsonPropertyName("relative")]
    public long? Relative {get; init;}
    [JsonPropertyName("total")]
    public long? Total {get; init;}
    [JsonPropertyName("used")]
    public required long Used {get; init;}
}