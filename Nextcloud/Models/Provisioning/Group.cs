namespace Nextcloud.Models.Provisioning;

public record Group
(
    string Id,
    string Displayname,
    bool Usercount,
    bool Disabled,
    bool CanAdd,
    bool CanRemove
);