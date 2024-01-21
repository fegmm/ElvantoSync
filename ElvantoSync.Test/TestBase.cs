using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Nextcloud;
using ElvantoSync.ElvantoApi;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nextcloud.Extensions;
using NextcloudApi;

namespace ElvantoSync.Tests;

public abstract class TestBase : IAsyncLifetime
{
     public abstract Task ApplyAsync_ShouldAddNewPersonsFromElvanto();
     public abstract Task ApplyAsync_ShouldNotAddIfNoNewPerson();
    private readonly NextcloudContainer nextcloud;
   
    protected ServiceCollection Services { get; private set; }
     private bool _disposed = false;
    public TestBase()
    {
        
        Services = new ServiceCollection();
        var nextcloud = new NextcloudContainer();
        ConfigureServices(nextcloud);
        this.nextcloud = nextcloud;
    }

     protected IServiceProvider BuildServiceProvider()
    {
        return Services.BuildServiceProvider();
    }

    protected virtual void ConfigureServices(NextcloudContainer nextcloud)
    {
        var settings = new Settings("./TestPath", null, null, null, null, null, null, null,null,null);
        Services.AddSingleton<PeopleToNextcloudSync>();
        //services.AddNextCloudSync();
        Services.AddSingleton<Settings>(settings);
        Services.AddNextcloudClients(nextcloud.NextcloudUrl, "admin", "StrongPassword123!", "elvatnosync/1.0");

    }

      public void Dispose()
    {
        // Dispose of unmanaged resources
        nextcloud.DisposeAsync();
        Dispose(true);
        
        // Suppress finalization
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects)
                if (Services is IDisposable serviceProviderDisposable)
                {
                    serviceProviderDisposable.Dispose();
                }

                // If any other IDisposable fields, dispose them here
                // e.g., _mockExternalDependency.Dispose();
            }

            // Free unmanaged resources (unmanaged objects) and override a finalizer below
            // Set large fields to null

            _disposed = true;
        }
    }

    public string? NextcloudUrl => nextcloud.NextcloudUrl;

    public async Task InitializeAsync() => await nextcloud.Start();

    public Task DisposeAsync() => Task.CompletedTask;
}
