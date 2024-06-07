using Azure;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.MySql;

namespace Nextcloud.Tests;

public class NextcloudHost : BackgroundService
{
    public string NextcloudUrl => $"http://nextcloud.local:{port}";

    private IContainer container;
    private MySqlContainer sql_container;
    private readonly ushort port;
    private readonly HttpClient client;

    public NextcloudHost(HttpClient client)
    {
        port = 80;
        this.client = client;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(NextcloudUrl);
        }
        catch (Exception)
        {
            response = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.ServiceUnavailable };
        }
        if (container != null || response.IsSuccessStatusCode)
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
            .WithAutoRemove(false)
            .WithReuse(true)
            .Build();

        await sql_container.StartAsync(cancellationToken);

        await sql_container.ExecAsync(["mysql", "-p", "mysql", "-e", "ALTER DATABASE nextcloud CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;"]);


        container = new ContainerBuilder()
            .WithImage("nextcloud-test:latest")
            .WithPortBinding(port, 80)
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .WithAutoRemove(false)
            .WithReuse(true)
            .Build();

        await container.StartAsync(cancellationToken);

        await container.ExecAsync(["su", "www-data", "-s", "/bin/bash", "-c", $"php occ db:convert-type --clear-schema --password mysql --port 3306 -n mysql mysql db nextcloud"], cancellationToken);
        await container.ExecAsync(["su", "www-data", "-s", "/bin/bash", "-c", $"php occ config:system:set mysql.utf8mb4 --type boolean --value=\"true\" && php occ maintenance:repair"], cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (container == null || sql_container == null)
        {
            return;
        }
        await container.DisposeAsync();
        await sql_container.DisposeAsync();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
