using System.Collections.Generic;

namespace ElvantoSync.Settings.ChurchTools;

internal record PeopleToChurchToolsSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:ChurchTools:PeopleToChurchToolsSync";

    public string CategoryToSync { get; set; }
    public HashSet<string> ExceptFromSync { get; set; } = [];
}
