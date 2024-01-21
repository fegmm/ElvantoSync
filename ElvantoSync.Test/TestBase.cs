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

namespace ElvantoSync.Tests;

public class TestBase : IAsyncLifetime, IClassFixture<NextcloudContainer>
{
    private readonly NextcloudContainer nextcloud;
    protected ServiceProvider ServiceProvider { get; }
     private Mock<IElvantoClient> _elvantoClientMock;

    public TestBase(NextcloudContainer nextcloud)
    {
        var settings = new Settings("./TestPath","","","","", "", "", "", "", "", false, false, false, false, false, false, false, false, false, false, false, false);
            
         _elvantoClientMock = new Mock<IElvantoClient>();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<PeopleToNextcloudSync>();
        //services.AddNextCloudSync();
        services.AddSingleton<Settings>(settings);
        services.AddNextcloudClients(nextcloud.NextcloudUrl, "admin", "StrongPassword123!", "elvatnosync/1.0");
        services.AddSingleton<IElvantoClient>(_elvantoClientMock.Object);
          var people = new List<Person> { new Person { Id = "1", Firstname ="Test", Lastname="Tester",Email = "MyEmail"  }, new Person { Id = "2", Firstname ="Test", Lastname="Tester",Email = "MyEmail" } };
        _elvantoClientMock.Setup(x => x.PeopleGetAllAsync(It.IsAny<GetAllPeopleRequest>()))
            .ReturnsAsync(new PeopleGetAllResponse { People = new People { Person = [.. people] } });

        ServiceProvider = services.BuildServiceProvider();
        this.nextcloud = nextcloud;
    }

    public string? NextcloudUrl => nextcloud.NextcloudUrl;

    public async Task InitializeAsync() => await nextcloud.Start();

    public Task DisposeAsync() => Task.CompletedTask;
}
