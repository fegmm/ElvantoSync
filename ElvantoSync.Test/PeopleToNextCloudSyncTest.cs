using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Interfaces;
using Xunit.Priority;
using ElvantoSync.Nextcloud;
using ElvantoSync.ElvantoService;
using ElvantoSync.ElvantoApi.Models;
using Moq;
using Nextcloud.Models.Provisioning;
using Nextcloud.Tests;


namespace ElvantoSync.Tests;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]

public class PeopleToNextcloudSyncTests : TestBase
{

    private ISync _peopleToNextcloudSync;
    private readonly INextcloudProvisioningClient client;
    private readonly IServiceProvider _serviceProvider;

    public PeopleToNextcloudSyncTests() : base()
    {

        _serviceProvider = BuildServiceProvider();



        client = _serviceProvider.GetRequiredService<INextcloudProvisioningClient>();
    }


    protected override void ConfigureServices(NextcloudContainer nextcloud)
    {
        base.ConfigureServices(nextcloud);
        _elvantoClientMock = new Mock<IElvantoClient>();
        Services.AddTransient<IElvantoClient>(x => _elvantoClientMock.Object);
    }

    [Fact, Priority(0)]
    public override async Task ApplyAsync_ShouldAddNewElementFromElvanto()
    {

        IEnumerable<Person> people = setUpPeopleMock();
       _peopleToNextcloudSync = fetchSyncImplementation<PeopleToNextcloudSync>(_serviceProvider);

        await _peopleToNextcloudSync.ApplyAsync();
        var result = await client.GetUsers();

        // Assert
        var users = result.Where(i => i.Id != "admin").ToArray();
        users.Should().HaveCount(people.Count());
        foreach (var user in users)
        {
            user.DisplayName.Should().NotBeNullOrEmpty();
            user.DisplayName.Should().ContainAny(people.Select(x => x.Firstname));
            user.DisplayName.Should().ContainAny(people.Select(x => x.Lastname));
            user.Email.Should().NotBeNullOrEmpty();
            user.Email.Should().ContainAny(people.Select(x => x.Email));
        }
    }

    

    [Fact, Priority(0)]
    public override async Task ApplyAsync_ShouldNotAddIfNoNewElement()
    {

        var people = setUpPeopleMock();
        _peopleToNextcloudSync = fetchSyncImplementation<PeopleToNextcloudSync>(_serviceProvider);
        await _peopleToNextcloudSync.ApplyAsync();
        var initialApply = await client.GetUsers();
        // Act
        
        await _peopleToNextcloudSync.ApplyAsync();
        var secondApply = await client.GetUsers();
        // Assert

        Assert.True(initialApply.Where(x => !x.DisplayName.Equals("admin"))
        .SequenceEqual(secondApply.Where(x => !x.DisplayName.Equals("admin"))));

    }





    // Similar tests can be written for GetToAsync, AddMissingAsync, RemoveAdditionalAsync and IsActive methods.
}