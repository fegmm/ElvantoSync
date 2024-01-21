namespace ElvantoSync.Settings.Nextcloud;

internal record GroupsToNextcloudSyncSettings : MappedSyncSettings
{
    public string GroupLeaderSuffix { get; init; } = "- Leitung";
}
