using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Nextcloud.Tests;

internal class TestBase : IAsyncLifetime
{
    private readonly IContainer container;

    public TestBase()
    {
       container = new ContainerBuilder()
            .WithImage("nextcloud:latest")
            .WithPortBinding(80, 80)
            .WithEnvironment("SQLITE_DATABASE", "db.sqlite")
            .WithEnvironment("NEXTCLOUD_ADMIN_USER", "admin")
            .WithEnvironment("NEXTCLOUD_ADMIN_PASSWORD", "securePassword123!")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .WithStartupCallback(async (container, ct) =>
            {
                await container.ExecAsync(["su", "www-data", "&&", "php", "occ", "app:install"]);
            })
            .Build();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public Task InitializeAsync() => this.container.StartAsync();
}
