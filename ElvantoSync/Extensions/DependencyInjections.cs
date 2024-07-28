using ElvantoSync;
using ElvantoSync.AllInkl;
using ElvantoSync.Application.Elvanto;
using ElvantoSync.GroupFinder;
using ElvantoSync.GroupFinder.service;
using ElvantoSync.GroupFinder.Service;
using ElvantoSync.Nextcloud;
using ElvantoSync.Settings;
using ElvantoSync.Settings.AllInkl;
using ElvantoSync.Settings.Elvanto;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http.Headers;
using System.Text;

public static class DependencyInjections
{
    public static IServiceCollection AddSyncs(this IServiceCollection services)
        => services
        .AddTransient<ISync, GroupFinderSync>()
        .AddTransient<ISync, PeopleToNextcloudSync>()
        .AddTransient<ISync, PeopleToNextcloudContactSync>()
        .AddTransient<ISync, GroupsToNextcloudSync>()
        .AddTransient<ISync, GroupsToNextcloudGroupFolderSync>()
        .AddTransient<ISync, DepartementsToGroupMemberSync>()
        .AddTransient<ISync, GroupsToCollectivesSync>()
        .AddTransient<ISync, GroupsToDeckSync>()
        .AddTransient<ISync, GroupsToTalkSync>()
        .AddTransient<ISync, GroupsToEmailSync>();

    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, string username, string password)
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

        byte[] authToken = Encoding.UTF8.GetBytes($"{username}:{password}");
        var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

        services.AddHttpClient<IGroupFinderService, GroupFinderService>(i =>
        {
            i.Timeout = TimeSpan.FromMinutes(5);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}
