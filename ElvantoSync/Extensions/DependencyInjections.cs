using ElvantoSync;
using ElvantoSync.AllInkl;
using ElvantoSync.Application.Elvanto;
using ElvantoSync.ChurchTools;
using ElvantoSync.GroupFinder;
using ElvantoSync.GroupFinder.service;
using ElvantoSync.GroupFinder.Service;
using ElvantoSync.Nextcloud;
using ElvantoSync.Settings;
using ElvantoSync.Settings.AllInkl;
using ElvantoSync.Settings.ChurchTools;
using ElvantoSync.Settings.Elvanto;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;
using System.Net.Http.Headers;
using System.Text;

public static class DependencyInjections
{
    public static IServiceCollection AddSyncs(this IServiceCollection services)
        => services
        .AddTransient<ISync, GroupsToEmailSync>()
        .AddTransient<ISync, PeopleToChurchToolsSync>()
        .AddTransient<ISync, SongsToChurchToolsSync>()
        .AddTransient<ISync, ChurchToolsGroupSync>()
        .AddTransient<ISync, GroupFinderSync>()
        .AddTransient<ISync, PeopleToNextcloudSync>()
        .AddTransient<ISync, PeopleToNextcloudContactSync>()
        .AddTransient<ISync, GroupsToNextcloudSync>()
        .AddTransient<ISync, GroupsToNextcloudGroupFolderSync>()
        .AddTransient<ISync, DepartementsToGroupMemberSync>()
        .AddTransient<ISync, GroupsToCollectivesSync>()
        .AddTransient<ISync, GroupsToDeckSync>()
        .AddTransient<ISync, GroupsToTalkSync>();

    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, string username, string password, string nextcloudUrl)
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

        services.AddOptions<GroupFinderToNextCloudSyncSettings>()
            .BindConfiguration(GroupFinderToNextCloudSyncSettings.ConfigSection);

        services.AddOptions<PeopleToChurchToolsSyncSettings>()
            .BindConfiguration(PeopleToChurchToolsSyncSettings.ConfigSection);

        services.AddOptions<SongsToChurchToolsSyncSettings>()
            .BindConfiguration(SongsToChurchToolsSyncSettings.ConfigSection);

        services.AddOptions<ChurchToolsGroupSyncSettings>()
            .BindConfiguration(ChurchToolsGroupSyncSettings.ConfigSection);

        byte[] authToken = Encoding.UTF8.GetBytes($"{username}:{password}");
        var auth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

        services.AddHttpClient<IGroupFinderService, GroupFinderService>(i =>
        {
            i.BaseAddress = new Uri(nextcloudUrl);
            i.Timeout = TimeSpan.FromMinutes(5);
            i.DefaultRequestHeaders.Authorization = auth;
            i.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddQuartzInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }
}
