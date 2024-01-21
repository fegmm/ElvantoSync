using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
namespace ElvantoSync.Tests;

class MyLogger : ILogger, IDisposable
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return this;
    }

    public void Dispose()
    {
        return;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        File.AppendAllText("diagnostic.log", formatter.Invoke(state, exception));
    }
}

public class NextcloudContainer : IAsyncDisposable
{
    public string NextcloudUrl => $"http://localhost:{port}";
    private IContainer? container;
    private readonly ushort port;

    public NextcloudContainer()
    {
        port = (ushort)Random.Shared.Next(ushort.MaxValue / 2, ushort.MaxValue);
    }

    public async Task Start()
    {
        if (container != null)
        {
            return;
        }
        //TestcontainersSettings.Logger = new MyLogger();

        //var images = new ImageFromDockerfileBuilder()
        //    .WithDockerfile("nextcloud.dockerfile")
        //    .WithName("nextcloud-test:latest")
        //    .Build();

        //await images.CreateAsync();

        container = new ContainerBuilder()
            .WithImage("nextcloud-test:latest")
            .WithPortBinding(port, 80)
            .WithEnvironment("SQLITE_DATABASE", "db.sqlite")
            .WithEnvironment("NEXTCLOUD_ADMIN_USER", "admin")
            .WithEnvironment("NEXTCLOUD_ADMIN_PASSWORD", "securePassword123!")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();
        

        await container.StartAsync();
    }

    public async Task Stop()
    {
        if (container != null)
        {
            return;
        }

        await container.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (container == null)
        {
            return;
        }
        await container.DisposeAsync();
    }
}
