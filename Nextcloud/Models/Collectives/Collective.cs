using Nextcloud.Models.Circles;

namespace Nextcloud.Models.Collectives;

public record Collective(int id,
    string CircleId,
    string Emoji,
    object TrashTimestamp,
    int PageMode,
    string Name,
    MemberLevels Level,
    int EditPermissionLevel,
    int SharePermissionLevel,
    bool CanEdit,
    bool CanShare,
    object ShareToken,
    bool ShareEditable,
    int UserPageOrder,
    bool UserShowRecentPages
);