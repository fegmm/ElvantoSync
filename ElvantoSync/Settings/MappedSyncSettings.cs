namespace ElvantoSync.Settings;

internal record MappedSyncSettings : SyncSettings
{
    public bool UseMapping { get; init; } = true;
    public bool StoreMapping { get; init; } = true;
}