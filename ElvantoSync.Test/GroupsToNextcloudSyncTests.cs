using ElvantoSync.ElvantoService;
using ElvantoSync.Nextcloud;
using ElvantoSync.Settings.Nextcloud;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nextcloud.Interfaces;
using Nextcloud.Tests;


namespace ElvantoSync.Tests;

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

    public override async Task Apply_ShouldAddNewElementFromElvanto()
    {

        IEnumerable<ElvantoApi.Models.Group> groups = SetUpGroupMock(SetUpPeopleMock().ToArray());
        _groupsToNextcloudSync = FetchSyncImplementation<GroupsToNextcloudSync>(_serviceProvider);
        await _groupsToNextcloudSync.Apply();

        var result = await client.GetGroups();

        result.Where(x => x.DisplayName.Equals(groups.First().Name) && x.Id.Equals(groups.First().Id))
            .Should().NotBeEmpty();

        result.Where(x => x.DisplayName.Equals(groups.First().Name) && x.Id.Equals(groups.First().Id))
            .Should().NotBeEmpty();
    }

    public override async Task Apply_ShouldNotAddIfNoNewElement()
    {

        IEnumerable<ElvantoApi.Models.Group> groups = SetUpGroupMock(SetUpPeopleMock().ToArray());
        _groupsToNextcloudSync = FetchSyncImplementation<GroupsToNextcloudSync>(_serviceProvider);
        await _groupsToNextcloudSync.Apply();
        var initialApply = await client.GetGroups();
        // Act

        await _groupsToNextcloudSync.Apply();
        var secondApply = await client.GetGroups();
        // Assert

        Assert.True(initialApply.Count() == secondApply.Count());

    }

    public async Task Apply_ShouldAddLeaderGroup()
    {

        IEnumerable<ElvantoApi.Models.Group> groups = SetUpGroupMock(SetUpPeopleMock().ToArray());
        _groupsToNextcloudSync = FetchSyncImplementation<GroupsToNextcloudSync>(_serviceProvider);
        await _groupsToNextcloudSync.Apply();

        var groupSettings = _serviceProvider.GetRequiredService<GroupsToNextcloudSyncSettings>();

        var result = await client.GetGroups();
        var leaderResult = result.Where(x => x.DisplayName.Contains(groupSettings.GroupLeaderSuffix));

        leaderResult.Should().HaveCount(groups.Select(x => !x.Name.Equals("admin")).Count());
    }
}