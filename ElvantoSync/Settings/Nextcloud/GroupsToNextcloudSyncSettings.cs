using System.Collections.Generic;

namespace ElvantoSync.Settings.Nextcloud;

public record GroupsToNextcloudSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:Nextcloud:GroupsToNextcloudSync";

    public string GroupLeaderSuffix { get; init; } = "- Leitung";
    public ICollection<int> ExcludedGroupTypeIds { get; set; } = [3];
    public ICollection<int> ExcludedGroupIds { get; set; } = [];
}
