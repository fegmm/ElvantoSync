using System.Text.Json.Serialization;

namespace Nextcloud.Models.Provisioning;

public record User(
    [property: JsonPropertyName("additional_mail")] string[] AdditionalMail,
    [property: JsonPropertyName("additional_mailScope")] string[] AdditionalMailScope,
    string Address,
    string AddressScope,
    string AvatarScope,
    string Backend,
    Backendcapabilities BackendCapabilities,
    string Biography,
    string BiographyScope,
    [property: JsonPropertyName("displayname")] string DisplayName,
    [property: JsonPropertyName("displaynameScope")] string DisplayNameScope,
    string Email,
    string EmailScope,
    bool Enabled,
    string Fediverse,
    string FediverseScope,
    string[] Groups,
    string Headline,
    string HeadlineScope,
    string Id,
    string Language,
    long LastLogin,
    string Locale,
    string Manager,
    [property: JsonPropertyName("notify_email ")] string NotifyEmail,
    string Organisation,
    string OrganisationScope,
    string Phone,
    string PhoneScope,
    string Profile_enabled,
    [property: JsonPropertyName("profile_enabledScope")] string ProfileEnabledScope,
    UserQuota Quota,
    string Role,
    string RoleScope,
    string StorageLocation,
    string[] Subadmin,
    string Twitter,
    string TwitterScope,
    string Website,
    string WebsiteScope
);

public record Backendcapabilities(
    bool SetDisplayName,
    bool SetPassword
);

public record UserQuota(
    long Free,
    long Quota,
    long Relative,
    long Total,
    long Used
);