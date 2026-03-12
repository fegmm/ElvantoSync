using Fegmm.Elvanto.Models;
using Fegmm.Elvanto.Groups.GetAllJson;
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
    public abstract Task Apply_ShouldAddNewElementFromElvanto();
    public abstract Task Apply_ShouldNotAddIfNoNewElement();

    protected ApplicationSettings Settings;
    private readonly NextcloudContainer nextcloud;
    protected Mock<ElvantoService.IElvantoClient> _elvantoClientMock;
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
            new Person { Id = "3", Firstname = "Alex", Lastname = "Johnson", Email = "alexj@example.com" },
            new Person { Id = "4", Firstname = "Chris", Lastname = "Smith", Email = "chriss@example.net" },
            new Person { Id = "5", Firstname = "Jordan", Lastname = "Brown", Email = "jordanb@example.org" },
            new Person { Id = "6", Firstname = "Jamie", Lastname = "Lee", Email = "jamiel@example.com" },
            new Person { Id = "7", Firstname = "Casey", Lastname = "Kim", Email = "caseyk@example.net" },
        ];

        _elvantoClientMock
            .Setup(x => x.PeopleGetAllAsync(It.IsAny<Fegmm.Elvanto.People.GetAllJson.GetAllPostRequestBody>()))
            .ReturnsAsync(people);

        return people;
    }

    protected IEnumerable<Group> SetupGroupMock(Person[] person)
    {
        GroupMembers groupMembers = new GroupMembers
        {
            Person = person.Select(x => new GroupMember { Id = x.Id, Firstname = x.Firstname, Lastname = x.Lastname, Email = x.Email })
            .Take(2)
            .Select(x => { x.Position = GroupMemberPositions.AssistantLeader; return x; })
            .ToList()
        };

        groupMembers.Person.Take(2).Select(x => x.Position = GroupMemberPositions.Leader);

        IEnumerable<Group> groups = new List<Group>
        {
            new Group { Id = "1", Name = "Group 1", People = groupMembers},
            new Group { Id = "2", Name = "Group 2", People = groupMembers}
        };

        _elvantoClientMock
            .Setup(x => x.GroupsGetAllAsync(It.IsAny<GetAllPostRequestBody>()))
            .ReturnsAsync(groups);

        return groups;
    }



    protected virtual void ConfigureServices(NextcloudContainer nextcloud)
    {
        Services.AddDbContext<ElvantoSync.Persistence.DbContext>(options => options.UseSqlite("Data Source=ElvantoSync.db"))
               .AddOptions()
               .AddSingleton(new Mock<IKasClient>().Object)
               .AddApplicationOptions("test", "test","http://group-finder:8080")
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
