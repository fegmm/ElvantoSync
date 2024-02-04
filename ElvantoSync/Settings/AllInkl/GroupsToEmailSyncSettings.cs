namespace ElvantoSync.Settings.AllInkl;

internal record GroupsToEmailSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:AllInkl:GroupsToEmailSync";

    public string UploadGroupMailAddressesToNextcloudPath { get; init; }
    public string KASDomain { get; init; }
}