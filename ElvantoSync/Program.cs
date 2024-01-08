using ElvantoSync.Nextcloud;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nextcloud.Extensions;
using System;
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
        var nextcloud = new NextcloudApi.Api(new NextcloudApi.Settings()
        {
            ServerUri = new Uri(settings.NextcloudServer),
            Username = settings.NextcloudUser,
            Password = settings.NextcloudPassword,
            ApplicationName = nameof(ElvantoSync),
            RedirectUri = new Uri(settings.NextcloudServer)
        });

        var services = new ServiceCollection()
            .AddSingleton<ILogger>(ConfigureLogging())
            .AddSingleton<Settings>(settings)
            .AddSingleton<NextcloudApi.Api>(nextcloud)
            .AddSingleton<ElvantoApi.Client>(elvanto)
            .AddNextcloud(nameof(ElvantoSync), settings.NextcloudServer, settings.NextcloudUser, settings.NextcloudPassword);

        var serviceProvider = services.BuildServiceProvider();

        var kas = new KasApi.Client(new KasApi.Requests.AuthorizeHeader(
            kas_login: settings.KASLogin,
            kas_auth_data: settings.KASAuthData,
            kas_auth_type: "plain"
        ));

        if (settings.SyncElvantoDepartementsToGroups)
            await serviceProvider.GetService<Elvanto.DepartementsToGroupMemberSync>().ApplyAsync();

        if (settings.SyncNextcloudPeople)
            await serviceProvider.GetService<Nextcloud.PeopleToNextcloudSync>().ApplyAsync();

        if (settings.SyncNextcloudContacts)
            await serviceProvider.GetService<Nextcloud.PeopleToNextcloudContactSync>().ApplyAsync();

        if (settings.SyncNextcloudGroups)
            await serviceProvider.GetService<Nextcloud.GroupsToNextcloudSync>().ApplyAsync();

        if (settings.SyncNextcloudGroupmembers)
            await serviceProvider.GetService<Nextcloud.GroupMembersToNextcloudSync>().ApplyAsync();

        if (settings.SyncNextcloudGroupfolders)
            await serviceProvider.GetService<Nextcloud.GroupsToNextcloudGroupFolderSync>().ApplyAsync();

        if (settings.SyncNextcloudDeck)
            await serviceProvider.GetService<Nextcloud.GroupsToDeckSync>().ApplyAsync();

        if (settings.SyncNextcloudCollectives)
            await serviceProvider.GetService<GroupsToCollectivesSync>().ApplyAsync();

        if (settings.SyncElvantoGroupsToKASMail)
        {
            await serviceProvider.GetService<AllInkl.GroupsToEmailSync>().ApplyAsync();
            await serviceProvider.GetService<AllInkl.GroupMembersToMailForwardMemberSync>().ApplyAsync();
        }
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
