using ElvantoSync.ElvantoApi.Models;
using ElvantoSync.ElvantoService;
using ElvantoSync.Nextcloud;
using ElvantoSync.Settings.Nextcloud;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nextcloud.Interfaces;
using Nextcloud.Tests;
using Xunit.Priority;


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
    public override async Task Apply_ShouldAddNewElementFromElvanto()
    {

        IEnumerable<ElvantoApi.Models.Group> groups =await SetupGroupsTests();
        await _groupsToNextcloudSync.Apply();
        var result = await client.GetGroups();
        
        groups.All(x => result.Any(y => x.Id.Equals(y.Id) && x.Name.Equals(y.DisplayName)))
        .Should().BeTrue();


        //result.Where(x => x.DisplayName.Equals(groups.First().Name) && x.Id.Equals(groups.First().Id))
            
    }
 [Fact, Priority(0)]
    public override async Task Apply_ShouldNotAddIfNoNewElement()
    {

        IEnumerable<ElvantoApi.Models.Group> groups = await SetupGroupsTests();
        await _groupsToNextcloudSync.Apply();
        var initialApply = await client.GetGroups();
        // Act

        await _groupsToNextcloudSync.Apply();
        var secondApply = await client.GetGroups();
        // Assert

        Assert.True(initialApply.Count() == secondApply.Count());

    }
 [Fact, Priority(0)]
    public async Task Apply_ShouldAddLeaderGroup()
    {
        IEnumerable<Group> groups = await SetupGroupsTests();
        var groupSettings = _serviceProvider.GetRequiredService<IOptions<GroupsToNextcloudSyncSettings>>().Value;
        await _groupsToNextcloudSync.Apply();
        var result = await client.GetGroups();
        var leaderResult = result.Where(x => x.DisplayName.Contains(groupSettings.GroupLeaderSuffix));

        leaderResult.Should().HaveCount(groups.Select(x => !x.Name.Equals("admin")).Count());
    }

    private async Task<IEnumerable<Group>> SetupGroupsTests()
    {
        IEnumerable<ElvantoApi.Models.Group> groups = SetupGroupMock(SetupPeopleMock().ToArray());
        _groupsToNextcloudSync = FetchSyncImplementation<GroupsToNextcloudSync>(_serviceProvider);
        _peopleToNextcloudSync = FetchSyncImplementation<PeopleToNextcloudSync>(_serviceProvider);
        await _peopleToNextcloudSync.Apply();   
        
        return groups;
    }
}