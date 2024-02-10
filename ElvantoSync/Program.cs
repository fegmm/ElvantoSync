using ElvantoSync.ElvantoService;
using ElvantoSync.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nextcloud.Extensions;

var builder = Host.CreateApplicationBuilder();

var settings = builder.Configuration.Get<ApplicationSettings>();

var elvanto = new ElvantoSync.ElvantoApi.Client(settings.ElvantoKey);
var kas = new KasApi.Client(new KasApi.Requests.AuthorizeHeader(
    kas_login: settings.KASLogin,
    kas_auth_data: settings.KASAuthData,
    kas_auth_type: "plain"
));

builder.Services
    .AddDbContext<DbContext>(options => options.UseSqlite(settings.ConnectionString))
    .AddOptions()

    .AddSingleton(elvanto)
    .AddSingleton(kas)
    .AddSingleton<IElvantoClient, ExternalClientWrapper>()

    .AddApplicationOptions()
    .AddNextcloudClients(settings.NextcloudServer, settings.NextcloudUser, settings.NextcloudPassword, nameof(ElvantoSync))
    .AddSyncs()

    .AddHostedService<ElvantoSync.ElvantoSync>();

builder.Build().Run();