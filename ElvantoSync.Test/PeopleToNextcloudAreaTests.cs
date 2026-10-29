using ElvantoSync.ChurchTools;
using ElvantoSync.ChurchTools.Models;
using ElvantoSync.Nextcloud;
using ElvantoSync.Settings.Nextcloud;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nextcloud.Interfaces;
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
}
