namespace Nextcloud.Models.GroupFolders;

[Flags]
public enum Permissions
{
    Read = 1,
    Update = 2,
    Create = 4,
    Delete = 8,
    Share = 0x10,
    All = 0x1F
}