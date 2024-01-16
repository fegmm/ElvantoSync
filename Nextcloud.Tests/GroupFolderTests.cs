using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Interfaces;
using Xunit.Priority;

namespace Nextcloud.Tests;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class GroupFolderTests : TestBase
{
    private readonly INextcloudGroupFolderClient client;
    private readonly string testGroupFolder;
    private readonly string testGroupFolderRenamed;

    public GroupFolderTests(NextcloudContainer nextcloud) : base(nextcloud)
    {
        client = ServiceProvider.GetRequiredService<INextcloudGroupFolderClient>();
        testGroupFolder = "testGroupFolder";
        testGroupFolderRenamed = "testGroupFolderRenamed";
    }

    [Fact, Priority(0)]
    public async void GetGroupFoldersReturnsEmpty()
    {
        var groupFolders = await client.GetGroupFolders();

        groupFolders.Should().BeEmpty();
    }

    [Fact, Priority(1)]
    public async void CreateGroupFolderReturnsCorrectId()
    {
        var id = await client.CreateGroupFolder(testGroupFolder);

        id.Should().Be(1);
    }

    [Fact, Priority(2)]
    public async void GetGroupFoldersReturnsGroupFolder()
    {
        var groupFolders = await client.GetGroupFolders();

        groupFolders.Should().Contain(gf => gf.MountPoint == testGroupFolder).And.HaveCount(1);
    }

    [Fact, Priority(3)]
    public async void SetMountpointSetsMountpoint()
    {
        var groupFolders = await client.GetGroupFolders();
        var groupFolderId = groupFolders.First(gf => gf.MountPoint == testGroupFolder).Id;

        await client.SetMountpoint(groupFolderId, testGroupFolderRenamed);

        groupFolders = await client.GetGroupFolders();
        groupFolders.Should().Contain(gf => gf.MountPoint == testGroupFolderRenamed).And.HaveCount(1);
    }

    [Fact, Priority(4)]
    public async void AddGroupAddsGroup()
    {
        var groupFolders = await client.GetGroupFolders();
        var groupFolderId = groupFolders.First(gf => gf.MountPoint == testGroupFolderRenamed).Id;

        await client.AddGroup(groupFolderId, "admin");

        groupFolders = await client.GetGroupFolders();
        var groupFolder = groupFolders.First(gf => gf.MountPoint == testGroupFolderRenamed);
        groupFolder.Groups.Keys.Should().Contain("admin");
    }

    [Fact, Priority(5)]
    public async void SetAclSetsAcl()
    {
        var groupFolders = await client.GetGroupFolders();
        var groupFolderId = groupFolders.First(gf => gf.MountPoint == testGroupFolderRenamed).Id;

        await client.SetAcl(groupFolderId, true);

        groupFolders = await client.GetGroupFolders();
        var groupFolder = groupFolders.First(gf => gf.MountPoint == testGroupFolderRenamed);
        groupFolder.Acl.Should().BeTrue();
    }

    [Fact, Priority(6)]
    public async void AddAclManagerAddsAclManager()
    {
        var groupFolders = await client.GetGroupFolders();
        var groupFolderId = groupFolders.First(gf => gf.MountPoint == testGroupFolderRenamed).Id;

        await client.AddAclManager(groupFolderId, "admin");

        groupFolders = await client.GetGroupFolders();
        var groupFolder = groupFolders.First(gf => gf.MountPoint == testGroupFolderRenamed);
    }

    [Fact, Priority(7)]
    public async void RemoveAclManagerRemovesAclManager()
    {
        var groupFolders = await client.GetGroupFolders();
        var groupFolderId = groupFolders.First(gf => gf.MountPoint == testGroupFolderRenamed).Id;

        await client.RemoveAclManager(groupFolderId, "admin");

        groupFolders = await client.GetGroupFolders();
        var groupFolder = groupFolders.First(gf => gf.MountPoint == testGroupFolderRenamed);
    }

    [Fact, Priority(8)]
    public async void DeleteGroupFolderDeletesGroupFolder()
    {
        var groupFolders = await client.GetGroupFolders();
        var groupFolderId = groupFolders.First(gf => gf.MountPoint == testGroupFolderRenamed).Id;

        await client.DeleteGroupFolder(groupFolderId);

        groupFolders = await client.GetGroupFolders();
        groupFolders.Should().BeEmpty();
    }
}
