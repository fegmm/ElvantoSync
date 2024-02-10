using ElvantoSync;
using ElvantoSync.AllInkl;
using ElvantoSync.Application.Elvanto;
using ElvantoSync.Nextcloud;
using ElvantoSync.Settings;
using ElvantoSync.Settings.AllInkl;
using ElvantoSync.Settings.Elvanto;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjections
{
    public static IServiceCollection AddSyncs(this IServiceCollection services)
        => services
            .AddTransient<ISync, GroupsToCollectivesSync>()
            .AddTransient<ISync, PeopleToNextcloudSync>()
            .AddSingleton<ISync, DepartementsToGroupMemberSync>()
            .AddSingleton<ISync, PeopleToNextcloudContactSync>()
            .AddSingleton<ISync, GroupsToNextcloudSync>()
            .AddSingleton<ISync, GroupsToNextcloudGroupFolderSync>()
            .AddSingleton<ISync, GroupsToDeckSync>()
            .AddSingleton<ISync, GroupsToNextcloudGroupFolderSync>()
            .AddSingleton<ISync, GroupsToEmailSync>()
            .AddSingleton<ISync, GroupsToTalkSync>();

    public static IServiceCollection AddApplicationOptions(this IServiceCollection services)
    {
        services.AddOptions<ApplicationSettings>()
            .BindConfiguration(ApplicationSettings.ConfigSection);
        services.AddOptions<GroupsToCollectiveSyncSettings>()
            .BindConfiguration(GroupsToCollectiveSyncSettings.ConfigSection);
        services.AddOptions<PeopleToNextcloudSyncSettings>()
            .BindConfiguration(PeopleToNextcloudSyncSettings.ConfigSection);
        services.AddOptions<DepartementsToGroupMemberSyncSettings>()
            .BindConfiguration(DepartementsToGroupMemberSyncSettings.ConfigSection);
        services.AddOptions<PeopleToContactSyncSettings>()
            .BindConfiguration(PeopleToContactSyncSettings.ConfigSection);
        services.AddOptions<GroupsToNextcloudSyncSettings>()
            .BindConfiguration(GroupsToNextcloudSyncSettings.ConfigSection);
        services.AddOptions<GroupsToNextcloudGroupFolderSyncSettings>()
            .BindConfiguration(GroupsToNextcloudGroupFolderSyncSettings.ConfigSection);
        services.AddOptions<GroupsToDeckSyncSettings>()
            .BindConfiguration(GroupsToDeckSyncSettings.ConfigSection);
        services.AddOptions<GroupsToTalkSyncSettings>()
            .BindConfiguration(GroupsToTalkSyncSettings.ConfigSection);
        services.AddOptions<GroupsToEmailSyncSettings>()
            .BindConfiguration(GroupsToEmailSyncSettings.ConfigSection);
        return services;
    }
}
