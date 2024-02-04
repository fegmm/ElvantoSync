using ElvantoSync.Extensions;
using ElvantoSync.ElvantoService;
using ElvantoSync.Settings.ApplicationSettings;
using ElvantoSync.Extensions;
using ElvantoSync.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nextcloud.Extensions;
using System.Linq;
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
            .Get<ApplicationSettings>();

        var elvanto = new ElvantoApi.Client(settings.ElvantoKey);
        var kas = new KasApi.Client(new KasApi.Requests.AuthorizeHeader(
            kas_login: settings.KASLogin,
            kas_auth_data: settings.KASAuthData,
            kas_auth_type: "plain"
        ));

        ServiceProvider services = BuildServiceProvider(settings, elvanto, kas);
        await ExecuteSync(services);
    }

    private static ServiceProvider BuildServiceProvider(ApplicationSettings settings, ElvantoApi.Client elvanto, KasApi.Client kas)
    {

        return new ServiceCollection()
            .AddSingleton<ILogger>(ConfigureLogging())
            .AddOptions()
            .AddApplicationOptions()
            .AddSingleton<ElvantoApi.Client>(elvanto)
            .AddSingleton<IElvantoClient, ExternalClientWrapper>()
            .AddSingleton<KasApi.Client>(kas)
            .AddSyncs()
            .AddNextcloudClients(settings.NextcloudServer, settings.NextcloudUser, settings.NextcloudPassword, nameof(ElvantoSync))
            .AddDbContext<DbContext>(options => options.UseSqlite(settings.ConnectionString))
            .BuildServiceProvider();
    }

    private static async Task ExecuteSync(ServiceProvider provider)
    {
        var services = provider.GetServices<ISync>()
        .Where(service => service.IsActive)
        .Select(service => service.Apply());
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
