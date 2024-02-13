using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.MySql;

namespace Nextcloud.Tests;

public class NextcloudHost : IHostedService
{
    public string NextcloudUrl => $"http://localhost:{port}";

    private IContainer? container;
    private MySqlContainer? sql_container;
    private readonly ushort port;

    public NextcloudHost()
    {
        port = 8080;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (container != null)
        {
            return;
        }

        var network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        sql_container = new MySqlBuilder()
            .WithDatabase("nextcloud")
            .WithNetwork(network)
            .WithNetworkAliases("db")
            .Build();

        await sql_container.StartAsync(cancellationToken);

        container = new ContainerBuilder()
            .WithImage("nextcloud-test:latest")
            .WithPortBinding(port, 80)
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();

        await container.StartAsync(cancellationToken);

        await container.ExecAsync(["su", "www-data", "-s", "/bin/bash", "-c", $"php occ db:convert-type --clear-schema --password mysql --port 3306 -n mysql mysql db nextcloud"], cancellationToken);

    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (container == null || sql_container == null)
        {
            return;
        }
        await container.DisposeAsync();
        await sql_container.DisposeAsync();

    }
}
