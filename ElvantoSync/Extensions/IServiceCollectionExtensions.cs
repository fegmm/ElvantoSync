using Microsoft.Extensions.DependencyInjection;

namespace ElvantoSync.Extensions;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services)
    {
        services.AddOptions<Settings.ApplicationSettings>()
            .BindConfiguration(Settings.ApplicationSettings.ConfigSection);
        services.AddOptions<Settings.Nextcloud.GroupsToCollectiveSyncSettings>()
            .BindConfiguration(Settings.Nextcloud.GroupsToCollectiveSyncSettings.ConfigSection);
        services.AddOptions<Settings.Nextcloud.PeopleToNextcloudSyncSettings>()
            .BindConfiguration(Settings.Nextcloud.PeopleToNextcloudSyncSettings.ConfigSection);
        services.AddOptions<Settings.Elvanto.DepartementsToGroupMemberSyncSettings>()
            .BindConfiguration(Settings.Elvanto.DepartementsToGroupMemberSyncSettings.ConfigSection);
        services.AddOptions<Settings.Nextcloud.PeopleToContactSyncSettings>()
            .BindConfiguration(Settings.Nextcloud.PeopleToContactSyncSettings.ConfigSection);
        services.AddOptions<Settings.Nextcloud.GroupsToNextcloudSyncSettings>()
            .BindConfiguration(Settings.Nextcloud.GroupsToNextcloudSyncSettings.ConfigSection);
        services.AddOptions<Settings.Nextcloud.GroupsToNextcloudGroupFolderSyncSettings>()
            .BindConfiguration(Settings.Nextcloud.GroupsToNextcloudGroupFolderSyncSettings.ConfigSection);
        services.AddOptions<Settings.Nextcloud.GroupsToDeckSyncSettings>()
            .BindConfiguration(Settings.Nextcloud.GroupsToDeckSyncSettings.ConfigSection);
        services.AddOptions<Settings.Nextcloud.GroupsToTalkSyncSettings>()
            .BindConfiguration(Settings.Nextcloud.GroupsToTalkSyncSettings.ConfigSection);
        services.AddOptions<Settings.AllInkl.GroupsToEmailSyncSettings>()
            .BindConfiguration(Settings.AllInkl.GroupsToEmailSyncSettings.ConfigSection);
        return services;
    }
}
