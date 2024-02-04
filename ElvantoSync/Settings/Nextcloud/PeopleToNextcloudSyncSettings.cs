namespace ElvantoSync.Settings.Nextcloud;

internal record PeopleToNextcloudSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:Nextcloud:PeopleToNextcloudSync";
    public string IdPrefix { get; init; } = "Elvanto-";
    public string Quoata { get; init; } = "1 GB";
}