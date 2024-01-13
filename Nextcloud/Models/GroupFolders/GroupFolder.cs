using System.Text.Json.Serialization;

namespace Nextcloud.Models.GroupFolders;

public record GroupFolder
(
    int Id,
    Dictionary<string, int> Groups,
    [property: JsonPropertyName("mount_point")] string MountPoint,
    long Quota,
    long Size,
    bool Acl,
    Dictionary<string, ACLManage> Manage,
    [property: JsonPropertyName("group_details")] Dictionary<string, GroupDetail> GroupDetails
);

public record ACLManage(
    string Type,
    string Id,
    string Displayname
);

public record GroupDetail(
    string DisplayName,
    int Permissions,
    string Type
);