namespace ElvantoSync.Settings.Nextcloud;

internal record PeopleToNextcloudSyncSettings : MappedSyncSettings
{
    public string IdPrefix { get; init; } = "Elvanto-";
    public string Quoata { get; init; } = "1 GB";
}