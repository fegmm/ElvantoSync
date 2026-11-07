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
using Nextcloud.Models.Provisioning;
using SyncDbContext = ElvantoSync.Persistence.DbContext;

namespace ElvantoSync.Tests;

public class PeopleToNextcloudAreaTests
{
    [Theory]
    [InlineData(new int[] { }, new string[] { "42", "43", "44" })]
    [InlineData(new int[] { 1 }, new string[] { "42" })]
    [InlineData(new int[] { 3, 5 }, new string[] { "42", "43" })]
    [InlineData(new int[] { 99 }, new string[] { })]
    public async Task IncludesPeopleInAnyConfiguredArea(int[] includedAreas, string[] expectedIds)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new SyncDbContext(new DbContextOptionsBuilder<SyncDbContext>().UseSqlite(connection).Options);
        var source = new Mock<IChurchToolsSource>();
        Person[] people =
        [
            new Person { Id = "42", AreaIds = new[] { 1, 3 } },
            new Person { Id = "43", AreaIds = new[] { 5 } },
            new Person { Id = "44" },
        ];
        source.Setup(s => s.GetPeopleAsync()).ReturnsAsync(people);
        var sync = new PeopleToNextcloudSync(source.Object, Mock.Of<INextcloudProvisioningClient>(), db,
            Options.Create(new PeopleToNextcloudSyncSettings { IncludedAreaIds = includedAreas }),
            NullLogger<PeopleToNextcloudSync>.Instance);

        var result = await sync.GetFromAsync();

        Assert.Equal(expectedIds, result.Select(p => p.Id));
        Assert.Equal(people, await source.Object.GetPeopleAsync());
    }

    [Fact]
    public async Task PreservesMigratedElvantoUsersAndCreatesNewUsersWithChurchToolsPrefix()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new SyncDbContext(new DbContextOptionsBuilder<SyncDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        const string migratedNextcloudId = "Elvanto-5bb86d7c-cefc-47d4-9444-a6f78dcd2add";
        db.IndexMappings.Add(new IndexMapping
        {
            FromId = "25",
            ToId = migratedNextcloudId,
            Type = nameof(PeopleToNextcloudSync),
        });
        await db.SaveChangesAsync();

        var source = new Mock<IChurchToolsSource>();
        source.Setup(s => s.GetPeopleAsync()).ReturnsAsync(
        [
            new Person { Id = "25", FirstName = "Existing", LastName = "Member", Email = "existing@example.org" },
            new Person { Id = "42", FirstName = "New", LastName = "Member", Email = "new@example.org" },
        ]);

        var provisioning = new Mock<INextcloudProvisioningClient>();
        provisioning.Setup(p => p.GetUsers()).ReturnsAsync(
        [
            CreateNextcloudUser(migratedNextcloudId, "Member, Existing", "existing@example.org"),
            CreateNextcloudUser("admin", "Administrator", "admin@example.org"),
        ]);
        provisioning.Setup(p => p.CreateUser(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateUserRequest r, CancellationToken _) => r.UserId);

        var sync = new PeopleToNextcloudSync(
            source.Object,
            provisioning.Object,
            db,
            Options.Create(new PeopleToNextcloudSyncSettings { LogOnly = false }),
            NullLogger<PeopleToNextcloudSync>.Instance);

        await sync.Apply();

        provisioning.Verify(p => p.CreateUser(It.Is<CreateUserRequest>(r => r.UserId == "ChurchTools-42")), Times.Once);
        provisioning.Verify(p => p.CreateUser(It.Is<CreateUserRequest>(r => r.UserId != "ChurchTools-42")), Times.Never);
        Assert.Equal("ChurchTools-42", db.ChurchToolsToNextcloudPeopleId("42"));
        Assert.Equal(migratedNextcloudId, db.ChurchToolsToNextcloudPeopleId("25"));
    }

    private static User CreateNextcloudUser(string id, string displayName, string email) => new()
    {
        Id = id,
        DisplayName = displayName,
        Email = email,
        AdditionalMail = [],
        Address = string.Empty,
        Backend = "Database",
        BackendCapabilities = new Backendcapabilities { SetDisplayName = true, SetPassword = true },
        Biography = string.Empty,
        Enabled = true,
        Fediverse = string.Empty,
        Groups = [],
        Headline = string.Empty,
        Language = "de",
        LastLogin = 0,
        Locale = "de_DE",
        Manager = string.Empty,
        Organisation = string.Empty,
        Phone = string.Empty,
        ProfileEnabled = "1",
        Quota = new UserQuota { Quota = 0, Used = 0 },
        Role = string.Empty,
        StorageLocation = string.Empty,
        Subadmin = [],
        Twitter = string.Empty,
        Website = string.Empty,
    };
}
