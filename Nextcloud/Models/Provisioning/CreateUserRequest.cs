namespace Nextcloud.Models.Provisioning;

public record CreateUserRequest(
    string UserId,
    string? DisplayName,
    string? Email,
    IEnumerable<string> Groups,
    IEnumerable<string> Subadmin,
    string? Manager,
    string? Language,
    string? Password,
    string? Quota
);