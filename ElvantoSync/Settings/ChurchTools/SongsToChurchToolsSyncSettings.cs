using System.Collections.Generic;

namespace ElvantoSync.Settings;

public record SongsToChurchToolsSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:ChurchTools:SongsToChurchToolsSync";

    public int? DefaultCategoryId { get; set; } = 37;

    public Dictionary<string, int?> CategoryMap { get; set; } = new()
    {
        ["f4e647d8-e2af-4820-a3a7-e573039454ef"] = 36
    };

    public bool DeleteAdditionalArrangements { get; set; } = true;
    public bool DeleteAdditionalFiles { get; set; } = true;
    public bool DeleteAdditionalTags { get; set; } = true;
}