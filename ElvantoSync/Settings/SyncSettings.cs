namespace ElvantoSync.Settings;

public record SyncSettings
{
    public string OutputFolder { get; init; } = "output";
    public bool LogOnly { get; init; } = false;
    public bool IsEnabled { get; init; } = true;
    public bool UseMapping { get; init; } = true;
    public bool StoreMapping { get; init; } = true;
    public bool EnableFallback { get; init; } = true;
    public bool AddMissing { get; set; } = true;
    public bool UpdateExisting { get; set; } = true;
    public bool DeleteAdditionals { get; set; } = false;
}