using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Nextcloud.Tests;

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

        var images = new ImageFromDockerfileBuilder()
            .WithName("nextcloud-test:latest")
            .WithDockerfile("nextcloud.dockerfile")
            .Build();

        await images.CreateAsync();

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

    public async ValueTask DisposeAsync()
    {
        if (container == null)
        {
            return;
        }
        await container.DisposeAsync();
    }
}
