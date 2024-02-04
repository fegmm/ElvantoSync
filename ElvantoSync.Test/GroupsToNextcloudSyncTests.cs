using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Interfaces;
using Xunit.Priority;
using ElvantoSync.ElvantoService;
using ElvantoSync.ElvantoApi.Models;
using Moq;
using Nextcloud.Models.Provisioning;
using Nextcloud.Tests;
using ElvantoSync.Application.Nextcloud;
using ElvantoSync.Application;


namespace ElvantoSync.Tests;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]

public class GroupsToNextcloudSyncTests : TestBase
{
    private ISync _peopleToNextcloudSync;
     private ISync _groupsToNextcloudSync;
    private readonly INextcloudProvisioningClient client;
    private readonly IServiceProvider _serviceProvider;

    public GroupsToNextcloudSyncTests() : base()
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
        
        IEnumerable<ElvantoApi.Models.Group> groups = setUpGroupMock();
        _groupsToNextcloudSync = fetchSyncImplementation<GroupsToNextcloudSync>(_serviceProvider);
        await _groupsToNextcloudSync.ApplyAsync();

        var result = await client.GetGroups();
      Assert.True(result.Where(x => 
        x.DisplayName.Equals(groups.First().Name) &&
        x.Id.Equals(groups.First().Id)
       ).Any()
       );
        Assert.True(result.Where(x => 
        x.DisplayName.Equals(groups.First().Name) &&
        x.Id.Equals(groups.First().Id)
       ).Any()
       );

    }

    


    [Fact, Priority(0)]
    public override async Task ApplyAsync_ShouldNotAddIfNoNewElement()
    {

        IEnumerable<ElvantoApi.Models.Group> groups = setUpGroupMock();
        _groupsToNextcloudSync = fetchSyncImplementation<GroupsToNextcloudSync>(_serviceProvider);
        await _groupsToNextcloudSync.ApplyAsync();
         var initialApply = await client.GetGroups();
        // Act
        
        await _groupsToNextcloudSync.ApplyAsync();
         var secondApply = await client.GetGroups();
        // Assert

        Assert.True(initialApply.Count() == secondApply.Count());

    }
[Fact, Priority(0)]
    public async Task ApplyAsync_ShouldAddLeaderGroup()
    {

        IEnumerable<ElvantoApi.Models.Group> groups = setUpGroupMock();
        _groupsToNextcloudSync = fetchSyncImplementation<GroupsToNextcloudSync>(_serviceProvider);
        await _groupsToNextcloudSync.ApplyAsync();

        var result = await client.GetGroups();
        var leaderResult = result.Where(x => 
        x.DisplayName.Contains(Settings.GroupLeaderSuffix)
       );

      Assert.True(leaderResult
       .Count() == groups.Select(x => !x.Name.Equals("admin")).Count()
       );
     

    }




    // Similar tests can be written for GetToAsync, AddMissingAsync, RemoveAdditionalAsync and IsActive methods.
}