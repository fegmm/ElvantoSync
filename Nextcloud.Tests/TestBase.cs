using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Extensions;

namespace Nextcloud.Tests;

public class TestBase : IAsyncLifetime, IClassFixture<NextcloudContainer>
{
    private readonly NextcloudContainer nextcloud;
    protected ServiceProvider ServiceProvider { get; }

    public TestBase(NextcloudContainer nextcloud)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddNextcloudClients(nextcloud.NextcloudUrl, "admin", "StrongPassword123!", "elvatnosync/1.0");
        ServiceProvider = services.BuildServiceProvider();
        this.nextcloud = nextcloud;
    }

    public string? NextcloudUrl => nextcloud.NextcloudUrl;

    public async Task InitializeAsync() => await nextcloud.Start();

    public Task DisposeAsync() => Task.CompletedTask;
}
