namespace ElvantoSync.Settings.AllInkl;

internal record GroupsToEmailSyncSettings : MappedSyncSettings
{
    public string UploadGroupMailAddressesToNextcloudPath { get; init; }
    public string KASDomain { get; init; }
}