using Microsoft.Extensions.DependencyInjection;
using ElvantoSync.Application.Nextcloud;
using ElvantoSync.Application.Elvanto;
using ElvantoSync.Application.AllInkl;
using ElvantoSync.Application;

public static class DependencyInjections
{
    public static IServiceCollection AddNextCloudSync(this IServiceCollection services)
    {
            
            services     
            .AddTransient<ISync, GroupsToCollectivesSync>()       
            .AddTransient<ISync, PeopleToNextcloudSync>()
            .AddSingleton<ISync, DepartementsToGroupMemberSync>()
            .AddSingleton<ISync, PeopleToNextcloudContactSync>()
            .AddSingleton<ISync, GroupsToNextcloudSync>()
            .AddSingleton<ISync, GroupMembersToNextcloudSync>()
            .AddSingleton<ISync, GroupsToNextcloudGroupFolderSync>()
            .AddSingleton<ISync, GroupsToDeckSync>()
            .AddSingleton<ISync, GroupsToNextcloudGroupFolderSync>()
            .AddSingleton<ISync, GroupsToEmailSync>()
            .AddSingleton<ISync, GroupMembersToMailForwardMemberSync>()
            .AddSingleton<ISync, GroupsToTalkSync>();
            return services;
        // Add any other dependencies related to NextCloudSync here
    }
}
