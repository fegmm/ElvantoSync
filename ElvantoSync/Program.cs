using ElvantoSync.Nextcloud;
using ElvantoSync.Nextcloud.Repository;
using ElvantoSync.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
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

        string username = settings.NextcloudUser;
        string password = settings.NextcloudPassword;
        string encoded = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));

        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri(settings.NextcloudServer);
        client.DefaultRequestHeaders.Add("OCS-APIRequest", "true");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
        var nextcloud_webdav = new WebDav.WebDavClient(client);

        var services = new ServiceCollection()
            .AddSingleton<ILogger>(ConfigureLogging())
            .AddSingleton<FlurlClientFactory>()
            .AddSingleton<Settings>(settings)
            .AddSingleton<NextcloudApi.Api>(nextcloud)
            .AddSingleton<ElvantoApi.Client>(elvanto)
            .AddSingleton(nextcloud_webdav)
            .AddTransient<ICircleRepository, CircleRepository>()
            .AddTransient<ICollectiveRepository, CollectivesRepository>()
            .BuildServiceProvider();

        var kas = new KasApi.Client(new KasApi.Requests.AuthorizeHeader()
        {
            kas_login = settings.KASLogin,
            kas_auth_data = settings.KASAuthData,
            kas_auth_type = "plain"
        });

        if (settings.SyncElvantoDepartementsToGroups)
            await services.GetService<Elvanto.DepartementsToGroupMemberSync>().ApplyAsync();

        if (settings.SyncNextcloudPeople)
            await services.GetService<Nextcloud.PeopleToNextcloudSync>().ApplyAsync();

        if (settings.SyncNextcloudContacts)
            await services.GetService<Nextcloud.PeopleToNextcloudContactSync>().ApplyAsync();

        if (settings.SyncNextcloudGroups)
            await services.GetService<Nextcloud.GroupsToNextcloudSync>().ApplyAsync();

        if (settings.SyncNextcloudGroupmembers)
            await services.GetService<Nextcloud.GroupMembersToNextcloudSync>().ApplyAsync();

        if (settings.SyncNextcloudGroupfolders)
            await services.GetService<Nextcloud.GroupsToNextcloudGroupFolderSync>().ApplyAsync();

        if (settings.SyncNextcloudDeck)
            await services.GetService<Nextcloud.GroupsToDeckSync>().ApplyAsync();

        if (settings.SyncNextcloudCollectives)
            await services.GetService<GroupsToCollectivesSync>().ApplyAsync();

        if (settings.SyncElvantoGroupsToKASMail)
        {
            await services.GetService<AllInkl.GroupsToEmailSync>().ApplyAsync();
            await services.GetService<AllInkl.GroupMembersToMailForwardMemberSync>().ApplyAsync();
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
