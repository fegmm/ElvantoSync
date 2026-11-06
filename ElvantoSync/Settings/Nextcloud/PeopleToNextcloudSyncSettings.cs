using System;
using System.Collections.Generic;

namespace ElvantoSync.Settings.Nextcloud;

public record PeopleToNextcloudSyncSettings : SyncSettings
{
    internal const string ConfigSection = "Sync:Nextcloud:PeopleToNextcloudSync";
    public const string LegacyIdPrefix = "Elvanto-";
    public string IdPrefix { get; init; } = "ChurchTools-";
    public string Quoata { get; init; } = "1 GB";
    public ICollection<int> IncludedAreaIds { get; init; } = [];

    public bool HasManagedPrefix(string id)
        => id.StartsWith(IdPrefix, StringComparison.Ordinal)
        || id.StartsWith(LegacyIdPrefix, StringComparison.Ordinal);

    public bool ContainsManagedPrefix(string value)
        => value.Contains(IdPrefix, StringComparison.Ordinal)
        || value.Contains(LegacyIdPrefix, StringComparison.Ordinal);
}
