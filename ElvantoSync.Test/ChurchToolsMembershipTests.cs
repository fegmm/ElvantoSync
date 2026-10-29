using ElvantoSync.ChurchTools;
using ElvantoSync.ChurchTools.Models;
using ElvantoSync.Nextcloud;
using ElvantoSync.Persistence.Entities;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nextcloud.Interfaces;
using SourceGroup = ElvantoSync.ChurchTools.Models.Group;
using TargetGroup = Nextcloud.Models.Provisioning.Group;
using SyncDbContext = ElvantoSync.Persistence.DbContext;

namespace ElvantoSync.Tests;

public class ChurchToolsMembershipTests
{
    [Fact]
    public async Task GroupAndLeaderMembershipUseMigratedUserIds()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new SyncDbContext(new DbContextOptionsBuilder<SyncDbContext>().UseSqlite(connection).Options);
        db.IndexMappings.AddRange(
            new IndexMapping { FromId = "42", ToId = "Elvanto-old-person", Type = nameof(PeopleToNextcloudSync) },
            new IndexMapping { FromId = "43", ToId = "Elvanto-other-person", Type = nameof(PeopleToNextcloudSync) });
        db.SaveChanges();
        var client = new Mock<INextcloudProvisioningClient>();
        client.Setup(c => c.GetMembers("old-group", default)).ReturnsAsync(new[] { "Elvanto-old-person", "stale-user" });
        client.Setup(c => c.GetMembers("old-group-leaders", default)).ReturnsAsync(new[] { "Elvanto-other-person" });
        var sync = new GroupsToNextcloudSync(Mock.Of<IChurchToolsSource>(), client.Object, db,
            Options.Create(new GroupsToNextcloudSyncSettings { GroupLeaderSuffix = "-leaders" }),
            NullLogger<GroupsToNextcloudSync>.Instance);
        var group = new SourceGroup
        {
            Id = "73", Name = "Example",
            Members = [new GroupMember { Id = "42", IsLeader = true }, new GroupMember { Id = "43" }],
        };

        await sync.UpdateMatches([(group, new TargetGroup { Id = "old-group", DisplayName = "Example", UserCount = 0, Disabled = false, CanAdd = true, CanRemove = true })]);

        client.Verify(c => c.AddUserToGroup("Elvanto-other-person", "old-group", default), Times.Once);
        client.Verify(c => c.RemoveUserFromGroup("stale-user", "old-group", default), Times.Once);
        client.Verify(c => c.AddUserToGroup("Elvanto-old-person", "old-group-leaders", default), Times.Once);
        client.Verify(c => c.RemoveUserFromGroup("Elvanto-other-person", "old-group-leaders", default), Times.Once);
        client.Verify(c => c.AddUserToGroup("Elvanto-old-person", "old-group", default), Times.Never);
        client.Verify(c => c.AddUserToGroup(It.IsIn("42", "43", "Elvanto-42", "Elvanto-43"), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task MissingUserMappingDoesNotRemoveExistingGroupMembers()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new SyncDbContext(new DbContextOptionsBuilder<SyncDbContext>().UseSqlite(connection).Options);
        var client = new Mock<INextcloudProvisioningClient>();
        client.Setup(c => c.GetMembers(It.IsAny<string>(), default)).ReturnsAsync(new[] { "legacy-user" });
        var sync = new GroupsToNextcloudSync(Mock.Of<IChurchToolsSource>(), client.Object, db,
            Options.Create(new GroupsToNextcloudSyncSettings()), NullLogger<GroupsToNextcloudSync>.Instance);
        var group = new SourceGroup { Id = "73", Name = "Example", Members = [new GroupMember { Id = "42" }] };

        await sync.UpdateMatches([(group, new TargetGroup { Id = "legacy-group", DisplayName = "Example", UserCount = 0, Disabled = false, CanAdd = true, CanRemove = true })]);

        client.Verify(c => c.AddUserToGroup(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        client.Verify(c => c.RemoveUserFromGroup(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }
}
