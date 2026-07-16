using System.Collections.Generic;

namespace ElvantoSync.Settings;

public record SyncSettings
{
    public string OutputFolder { get; init; } = "output";
    public bool LogOnly { get; init; } = false;
    public bool IsEnabled { get; init; } = true;
    public bool AddMissing { get; set; } = true;
    public bool UpdateExisting { get; set; } = true;
    public bool DeleteAdditionals { get; set; } = false;
    public bool UseFallbackSync { get; set; } = false;
    public ICollection<string> ExcludedFromIds { get; set; } = [];
    public ICollection<string> ExcludedToIds { get; set; } = [];
}