using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Interfaces;
using Nextcloud.Models.Provisioning;
using Xunit.Priority;

namespace Nextcloud.Tests;


[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class ProvisioningTests : TestBase
{
    private readonly INextcloudProvisioningClient client;
    private readonly string testGroup;
    private readonly CreateUserRequest testUserProps;

    public ProvisioningTests(NextcloudContainer nextcloud) : base(nextcloud)
    {
        client = ServiceProvider.GetRequiredService<INextcloudProvisioningClient>();

        testGroup = "testGroup";
        testUserProps = new CreateUserRequest
        {
            UserId = "test",
            DisplayName = "DisplayName",
            Email = "test@example.org",
            Groups = ["admin"],
            Subadmin = [testGroup],
            Manager = "admin",
            Language = "en",
            Password = "testPasswort!123",
            Quota = "1 GB"
        };
    }

    [Fact, Priority(0)]
    public async void GetUsersReturnsAdmin()
    {
        var users = await client.GetUsers();

        users.Should().Contain(u => u.Id == "admin").And.HaveCount(1);
    }

    [Fact, Priority(0)]
    public async void GetGroupsReturnsAdmin()
    {
        var groups = await client.GetGroups();

        groups.Should().Contain(g => g.Id == "admin").And.HaveCount(1);
    }

    [Fact, Priority(1)]
    public async void CreateGroupReturnsCorrectGroupId()
    {
        await client.CreateGroup(testGroup, testGroup);
        var groups = await client.GetGroups();

        groups.Should().Contain(g => g.Id == testGroup).And.HaveCount(2);
    }

    [Fact, Priority(2)]
    public async void CreateUserReturnsCorrectUserId()
    {
        var userId = await client.CreateUser(testUserProps);


        userId.Should().NotBeNull();
        userId.Should().Be(testUserProps.UserId);

        var users = await client.GetUsers();
        users.Should().Contain(u => u.Id == testUserProps.UserId).And.HaveCount(2);

        var user = users.First(u => u.Id == testUserProps.UserId);
        user.DisplayName.Should().Be(testUserProps.DisplayName);
        user.Email.Should().Be(testUserProps.Email);
        user.Groups.Should().BeEquivalentTo(testUserProps.Groups);
        user.Subadmin.Should().BeEquivalentTo(testUserProps.Subadmin);
        user.Manager.Should().Be(testUserProps.Manager);
        user.Language.Should().Be(testUserProps.Language);
        user.Quota.Quota.Should().Be(1_073_741_824);
    }

    [Fact, Priority(3)]
    public async void EditUserChangesValues()
    {
        await client.EditUser(testUserProps.UserId, new EditUserRequest()
        {
            DisplayName = "test2",
            Email = "test2@example.org",
            Quota = "2 GB",
            Phone = "123456789",
            Address = "test street 1",
            Website = "https://example.org",
            Twitter = "@test"
        });

        var users = await client.GetUsers();
        users.Should().Contain(u => u.Id == testUserProps.UserId).And.HaveCount(2);

        var user = users.First(u => u.Id == testUserProps.UserId);
        user.DisplayName.Should().Be("test2");
        user.Email.Should().Be("test2@example.org");
        //user.Phone.Should().Be("123456789");
        //user.Address.Should().Be("test street 1");
        //user.Website.Should().Be("https://example.org");
        //user.Twitter.Should().Be("@test");
        user.Quota.Quota.Should().Be(2147483648);
    }

    [Fact, Priority(3)]
    public async void EditGroupChangesValues()
    {
        await client.EditGroup(testGroup, "testGroup2");


        var groups = await client.GetGroups();
        groups.Should().Contain(g => g.Id == testGroup).And.HaveCount(2);

        var group = groups.First(g => g.Id == testGroup);
        group.DisplayName.Should().Be("testGroup2");
    }

    [Fact, Priority(4)]
    public async void AddUserToGroupAddsUser()
    {
        await client.AddUserToGroup(testUserProps.UserId, testGroup);


        var users = await client.GetMembers(testGroup);
        users.Should().Contain(u => u == testUserProps.UserId).And.HaveCount(1);
    }

    [Fact, Priority(5)]
    public async void RemoveUserFromGroupRemovesUser()
    {
        await client.RemoveUserFromGroup(testUserProps.UserId, testGroup);


        var users = await client.GetMembers(testGroup);
        users.Should().NotContain(u => u == testUserProps.UserId).And.HaveCount(0);
    }

    [Fact, Priority(6)]
    public async void DeleteUserRemovesUser()
    {
        await client.DeleteUser(testUserProps.UserId);


        var users = await client.GetUsers();
        users.Should().NotContain(u => u.Id == testUserProps.UserId).And.HaveCount(1);
    }

    [Fact, Priority(6)]
    public async void DeleteGroupRemovesGroup()
    {
        await client.DeleteGroup(testGroup);


        var groups = await client.GetGroups();
        groups.Should().NotContain(g => g.Id == testGroup).And.HaveCount(1);
    }
}