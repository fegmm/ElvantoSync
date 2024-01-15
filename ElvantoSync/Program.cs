using ElvantoSync.AllInkl;
using ElvantoSync.Elvanto;
using ElvantoSync.Nextcloud;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nextcloud.Extensions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ElvantoSync;

class Program
{
    static async Task Main(string[] args)
    {
        var settings = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build()
            .Get<Settings>();

        var elvanto = new ElvantoApi.Client(settings.ElvantoKey);
        var kas = new KasApi.Client(new KasApi.Requests.AuthorizeHeader(
            kas_login: settings.KASLogin,
            kas_auth_data: settings.KASAuthData,
            kas_auth_type: "plain"
        ));
        
        ServiceProvider services = BuildServiceProvider(settings, elvanto, kas);
        await ExecuteSync(services);
 }

    private static ServiceProvider BuildServiceProvider(Settings settings, ElvantoApi.Client elvanto, KasApi.Client kas)
    {
        
        return new ServiceCollection()
            .AddSingleton<ILogger>(ConfigureLogging())
            .AddSingleton<Settings>(settings)
            .AddSingleton<ElvantoApi.Client>(elvanto)
            .AddSingleton<KasApi.Client>(kas)
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
            .AddSingleton<ISync, GroupsToTalkSync>()
            .AddNextcloud(settings.NextcloudServer, settings.NextcloudUser, settings.NextcloudPassword,nameof(ElvantoSync))
            .BuildServiceProvider();
    }

    private static async Task ExecuteSync(ServiceProvider provider)
    {
        var services = provider.GetServices<ISync>()
        .Where(service => service.IsActive())
        .Select(service => service.ApplyAsync());
        await Task.WhenAll(services);
    }
    private static ILogger ConfigureLogging()
    {

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("ElvantoSync", LogLevel.Debug)
                .AddConsole();
        });

        return loggerFactory.CreateLogger<Program>();
    }
}
