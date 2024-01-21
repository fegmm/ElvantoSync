namespace ElvantoSync.Settings;

internal record SyncSettings
{
    public string OutputFolder { get; init; } = "output";
    public bool LogOnly { get; init; } = false;
    public bool IsEnabled { get; init; } = true;
}