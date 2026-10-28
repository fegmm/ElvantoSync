using ElvantoSync;
using ElvantoSync.AllInkl;
using ElvantoSync.Nextcloud;
using ElvantoSync.Settings;
using ElvantoSync.Settings.AllInkl;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

public static class DependencyInjections
{
    public static IServiceCollection AddSyncs(this IServiceCollection services)
        => services
        .AddTransient<ISync, PeopleToNextcloudSync>()
        .AddTransient<ISync, PeopleToNextcloudContactSync>()
        .AddTransient<ISync, GroupsToNextcloudSync>()
        .AddTransient<ISync, GroupsToEmailSync>()
        .AddTransient<ISync, GroupsToNextcloudGroupFolderSync>()
        .AddTransient<ISync, GroupsToCollectivesSync>()
        .AddTransient<ISync, GroupsToDeckSync>()
        .AddTransient<ISync, GroupsToTalkSync>();

    public static IServiceCollection AddApplicationOptions(this IServiceCollection services)
    {
        services.AddOptions<ApplicationSettings>()
            .BindConfiguration(ApplicationSettings.ConfigSection);

        services.AddOptions<GroupsToCollectiveSyncSettings>()
            .BindConfiguration(GroupsToCollectiveSyncSettings.ConfigSection);

        services.AddOptions<PeopleToNextcloudSyncSettings>()
            .BindConfiguration(PeopleToNextcloudSyncSettings.ConfigSection);

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
