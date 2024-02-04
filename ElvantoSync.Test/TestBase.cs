using ElvantoSync.Application;
using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.Nextcloud;
using KasApi;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nextcloud.Extensions;
using Nextcloud.Tests;

namespace ElvantoSync.Tests;

public abstract class TestBase : IAsyncLifetime
{
    public abstract Task ApplyAsync_ShouldAddNewElementFromElvanto();
    public abstract Task ApplyAsync_ShouldNotAddIfNoNewElement();

    protected Settings Settings;
    private readonly NextcloudContainer nextcloud;
    protected Mock<ElvantoService.IElvantoClient> _elvantoClientMock;
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

    protected IEnumerable<Person> setUpPeopleMock()
    {
        IEnumerable<Person> people = new List<Person>
        {
            new Person { Id = "1", Firstname = "Test", Lastname = "Tester", Email = "myemail@example.org" },
            new Person { Id = "2", Firstname = "Test", Lastname = "Tester", Email = "myemail@example.org" }
        };

        _elvantoClientMock
            .Setup(x => x.PeopleGetAllAsync(It.IsAny<GetAllPeopleRequest>()))
            .ReturnsAsync(new PeopleGetAllResponse
            {
                People = new People
                {
                    Person = people.ToArray()
                }
            });

        return people;
    }

    protected IEnumerable<Group> setUpGroupMock()
    {
       

        // GroupMembers groupMembers = new GroupMembers
        // {
        //     Person = person.Select(x => new GroupMember { Id = x.Id, Firstname = x.Firstname, Lastname = x.Lastname, Email = x.Email}).ToArray()
        // };
        IEnumerable<Group> groups = new List<Group>
        {
            new Group { Id = "1", Name = "Group 1"},
            new Group { Id = "2", Name = "Group 2"}
        };

        _elvantoClientMock
            .Setup(x => x.GroupsGetAllAsync(It.IsAny<GetAllRequest>()))
            .ReturnsAsync(new GroupsGetAllResponse
            {
                Groups = new Groups
                {
                    Group = groups.ToArray()
                }
            });

        return groups;
    }

    

    protected virtual void ConfigureServices(NextcloudContainer nextcloud)
    {
        Settings = new Settings("./TestPath", null, null, null, null, null, null, null,"Leader",null);
       // Services.AddSingleton<PeopleToNextcloudSync>();
        Services.AddNextCloudSync();
        Services.AddSingleton<IKasClient >(new Mock<IKasClient>().Object);
        Services.AddSingleton<Settings>(Settings);
        Services.AddNextcloudClients(nextcloud.NextcloudUrl, "admin", "StrongPassword123!", "elvatnosync/1.0");

    }

    protected ISync fetchSyncImplementation<T>(IServiceProvider serviceProvider)
    {
        // Act
       return serviceProvider.GetServices<ISync>()
        .Where(service => service.GetType().Equals(typeof(T)))
        .Select(service => service).First();
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
