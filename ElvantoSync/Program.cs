using ElvantoSync.AllInkl;
using ElvantoSync.Elvanto;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.Nextcloud;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebDav;

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
        var kas = new KasApi.Client(new KasApi.Requests.AuthorizeHeader()
        {
            kas_login = settings.KASLogin,
            kas_auth_data = settings.KASAuthData,
            kas_auth_type = "plain"
        });
        ServiceProvider services = BuildServiceProvider(settings, elvanto, kas);
        await ExecuteSync(services);




    }

    private static ServiceProvider BuildServiceProvider(Settings settings, ElvantoApi.Client elvanto, KasApi.Client kas)
    {
        return new ServiceCollection()
            .AddSingleton<ILogger>(ConfigureLogging())
            .AddSingleton<FlurlClientFactory>()
            .AddSingleton<Settings>(settings)
            .AddSingleton<NextcloudApi.Api>(nextcloud)
            .AddSingleton<ElvantoApi.Client>(elvanto)
            .AddSingleton(nextcloud_webdav)
            .AddSingleton<KasApi.Client>(kas)
            .AddSingleton<ISync, GroupsToCollectivesSync>()
            .AddSingleton<ISync, PeopleToNextcloudSync>()
            .AddSingleton<ISync, DepartementsToGroupMemberSync>()
            .AddSingleton<ISync, PeopleToNextcloudContactSync>()
            .AddSingleton<ISync, GroupsToNextcloudSync>()
            .AddSingleton<ISync, GroupMembersToNextcloudSync>()
            .AddSingleton<ISync, GroupsToNextcloudGroupFolderSync>()
            .AddSingleton<ISync, GroupsToDeckSync>()
            .AddSingleton<ISync, GroupsToNextcloudGroupFolderSync>()
            .AddSingleton<ISync, GroupsToEmailSync>()
            .AddSingleton<ISync, GroupMembersToMailForwardMemberSync>()
            .AddTransient<ICircleRepository, CircleRepository>()
            .AddTransient<ICollectiveRepository, CollectivesRepository>()
            .AddNextcloud(nameof(ElvantoSync), settings.NextcloudServer, settings.NextcloudUser, settings.NextcloudPassword);
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
