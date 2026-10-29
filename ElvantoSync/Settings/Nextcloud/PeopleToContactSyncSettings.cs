using System.Collections.Generic;

namespace ElvantoSync.Settings.Nextcloud;

internal record PeopleToContactSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:Nextcloud:PeopleToContactSync";

    public string ContactBook { get; init; } = "default";
    public ICollection<int> IncludedAreaIds { get; init; } = [];
}
