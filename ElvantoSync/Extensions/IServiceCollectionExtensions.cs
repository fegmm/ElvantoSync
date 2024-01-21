using Microsoft.Extensions.DependencyInjection;

namespace ElvantoSync.Extensions;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSyncOptions(this IServiceCollection services)
    {
        services.AddOptions<Settings.ApplicationSettings>();
        services.AddOptions<Settings.Nextcloud.GroupsToCollectiveSyncSettings>();
        services.AddOptions<Settings.Nextcloud.PeopleToNextcloudSyncSettings>();
        services.AddOptions<Settings.Elvanto.DepartementsToGroupMemberSyncSettings>();
        services.AddOptions<Settings.Nextcloud.PeopleToContactSyncSettings>();
        services.AddOptions<Settings.Nextcloud.GroupsToNextcloudSyncSettings>();
        services.AddOptions<Settings.Nextcloud.GroupsToNextcloudGroupFolderSyncSettings>();
        services.AddOptions<Settings.Nextcloud.GroupsToDeckSyncSettings>();
        services.AddOptions<Settings.Nextcloud.GroupsToNextcloudGroupFolderSyncSettings>();
        services.AddOptions<Settings.Nextcloud.GroupsToTalkSyncSettings>();
        services.AddOptions<Settings.AllInkl.GroupsToEmailSyncSettings>();
        return services;
    }
}
