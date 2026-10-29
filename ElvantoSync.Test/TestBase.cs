using ElvantoSync.ChurchTools.Models;
using ElvantoSync.Settings;
using KasApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Nextcloud.Extensions;
using Nextcloud.Tests;

namespace ElvantoSync.Tests;

public abstract class TestBase : IAsyncLifetime
{
    public abstract Task Apply_ShouldAddNewElementFromChurchTools();
    public abstract Task Apply_ShouldNotAddIfNoNewElement();

    protected ApplicationSettings Settings;
    private readonly NextcloudContainer nextcloud;
    protected Mock<ChurchTools.IChurchToolsSource> _churchToolsSourceMock;
    protected IServiceCollection Services { get; private set; }
    private bool _disposed = false;
    public TestBase()
    {

        Services = new ServiceCollection();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddJsonFile("application.json");
        Services = builder.Services;
        var nextcloud = new NextcloudContainer();

        ConfigureServices(nextcloud);

        this.nextcloud = nextcloud;
    }

    protected IServiceProvider BuildServiceProvider()
    {
        return Services.BuildServiceProvider();
    }

    protected IEnumerable<Person> SetupPeopleMock()
    {
        IEnumerable<Person> people = [
            new Person { Id = "3", FirstName = "Alex", LastName = "Johnson", Email = "alexj@example.com" },
            new Person { Id = "4", FirstName = "Chris", LastName = "Smith", Email = "chriss@example.net" },
            new Person { Id = "5", FirstName = "Jordan", LastName = "Brown", Email = "jordanb@example.org" },
            new Person { Id = "6", FirstName = "Jamie", LastName = "Lee", Email = "jamiel@example.com" },
            new Person { Id = "7", FirstName = "Casey", LastName = "Kim", Email = "caseyk@example.net" },
        ];

        _churchToolsSourceMock
            .Setup(x => x.GetPeopleAsync())
            .ReturnsAsync(people);

        return people;
    }

    protected IEnumerable<Group> SetupGroupMock(Person[] person)
    {
        var groupMembers = person.Take(2).Select(x => new GroupMember
        {
            Id = x.Id, Email = x.Email, IsLeader = true
        }).ToList();

        IEnumerable<Group> groups = new List<Group>
        {
            new Group { Id = "1", Name = "Group 1", Members = groupMembers},
            new Group { Id = "2", Name = "Group 2", Members = groupMembers}
        };

        _churchToolsSourceMock
            .Setup(x => x.GetGroupsAsync())
            .ReturnsAsync(groups);

        return groups;
    }



    protected virtual void ConfigureServices(NextcloudContainer nextcloud)
    {
        Services.AddDbContext<ElvantoSync.Persistence.DbContext>(options => options.UseSqlite("Data Source=ElvantoSync.db"))
               .AddOptions()
               .AddSingleton(new Mock<IKasClient>().Object)
               .AddApplicationOptions()
               .AddNextcloudClients(nextcloud.NextcloudUrl, "admin", "StrongPassword123!", "elvatnosync/1.0")
               .AddChurchToolsClient((config, _) =>
                {
                    config.BaseUrl = "https://demo.church.tools/api";
                    config.ApiToken = "Login InvalidToken";
                })
               .AddSyncs();
    }

    protected ISync FetchSyncImplementation<T>(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetServices<ISync>()
                 .Where(service => service is T)
                 .Select(service => service).First();
    }

    public string? NextcloudUrl => nextcloud.NextcloudUrl;

    public async Task InitializeAsync() => await nextcloud.Start();

    public Task DisposeAsync() => Task.CompletedTask;
}
