using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.MySql;

namespace Nextcloud.Tests;

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
    private MySqlContainer? sql_container;
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

        var network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        sql_container = new MySqlBuilder("mysql:8.0")
            .WithDatabase("nextcloud")
            .WithNetwork(network)
            .WithNetworkAliases("db")
            .Build();

        await sql_container.StartAsync();

        container = new ContainerBuilder("nextcloud-test:latest")
            .WithPortBinding(port, 80)
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(80))
            .Build();

        await container.StartAsync();

        await container.ExecAsync(["su", "www-data", "-s", "/bin/bash", "-c", $"php occ db:convert-type --clear-schema --password mysql --port 3306 -n mysql mysql db nextcloud"]);
    }

    public async ValueTask DisposeAsync()
    {
        if (container == null || sql_container == null)
        {
            return;
        }
        await container.DisposeAsync();
        await sql_container.DisposeAsync();
    }
}
