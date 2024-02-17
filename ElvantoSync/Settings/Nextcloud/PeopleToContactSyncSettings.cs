namespace ElvantoSync.Settings.Nextcloud;

internal record PeopleToContactSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:Nextcloud:PeopleToContactSync";

    public string ContactBook { get; init; } = "kontakte";
}
