namespace ElvantoSync.Settings.Nextcloud;

public record GroupsToNextcloudSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:Nextcloud:GroupsToNextcloudSync";

    public string GroupLeaderSuffix { get; init; } = "- Leitung";
}
