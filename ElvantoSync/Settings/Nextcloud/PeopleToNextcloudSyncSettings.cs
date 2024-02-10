namespace ElvantoSync.Settings.Nextcloud;

public record PeopleToNextcloudSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:Nextcloud:PeopleToNextcloudSync";
    public string IdPrefix { get; init; } = "Elvanto-";
    public string Quoata { get; init; } = "1 GB";
}