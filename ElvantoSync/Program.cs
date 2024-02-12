using ElvantoSync;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nextcloud.Extensions;
using Quartz;
using System.Collections;
using System.Collections.Generic;

var builder = Host.CreateApplicationBuilder();

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
    .AddSingleton(kas)
    .AddSingleton<IElvantoClient, ExternalClientWrapper>()

    .AddApplicationOptions()
    .AddNextcloudClients(appSettings.NextcloudServer, appSettings.NextcloudUser, appSettings.NextcloudPassword, nameof(ElvantoSync))
    .AddSyncs();


if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Debug);
    var host = builder.Build();
    using (IServiceScope scope = host.Services.CreateScope())
    {
        var syncs = scope.ServiceProvider.GetService<IEnumerable<ISync>>();
        await new ElvantoSync.ElvantoSync(syncs).Execute(null);
    }
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