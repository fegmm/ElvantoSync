using System;
using ElvantoSync.ElvantoService;
using ElvantoSync.Settings;
using KasApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nextcloud.Extensions;
using Polly;
using Quartz;

var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddUserSecrets<Program>();
var appSettings = builder.Configuration
    .GetRequiredSection(ApplicationSettings.ConfigSection)
    .Get<ApplicationSettings>();

var elvanto = new ElvantoSync.ElvantoApi.Client(appSettings.ElvantoKey);
var kas = new KasApi.Client(new KasApi.Requests.AuthorizeHeader(
    kas_login: appSettings.KASLogin,
    kas_auth_data: appSettings.KASAuthData,
    kas_auth_type: "plain"
));

builder.Services
    .AddDbContext<ElvantoSync.Persistence.DbContext>(options => options.UseSqlite(appSettings.ConnectionString))
    .AddOptions()
    .AddSingleton(elvanto)
    .AddSingleton<IKasClient>(kas)
    .AddSingleton<IElvantoClient, ExternalClientWrapper>()
    .AddApplicationOptions(appSettings.NextcloudUser, appSettings.NextcloudPassword)
    .AddNextcloudClients(appSettings.NextcloudServer, appSettings.NextcloudUser, appSettings.NextcloudPassword, nameof(ElvantoSync))
    .AddSyncs();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Debug);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    builder.Services.AddHostedService<Nextcloud.Tests.NextcloudHost>();
    builder.Services.AddHostedService<ElvantoSync.HostedElvantoSync>();

    builder.Build().Run();
}
else
{
    builder.Services.AddQuartz(configure =>
    {
        var jobKey = new JobKey(nameof(ElvantoSync.ElvantoSync));
        configure
            .AddJob<ElvantoSync.ElvantoSync>(jobKey)
            .AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithCronSchedule(appSettings.CronSchedule)
            );
    })
    .AddQuartzHostedService(configure => configure.WaitForJobsToComplete = true);
    builder.Build().Run();
}