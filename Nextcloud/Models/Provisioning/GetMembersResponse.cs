namespace Nextcloud.Models.Provisioning;

internal record GetMembersResponse(
    IEnumerable<string> Users
);
