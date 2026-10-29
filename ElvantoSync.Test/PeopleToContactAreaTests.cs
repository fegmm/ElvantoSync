using ElvantoSync.ChurchTools;
using ElvantoSync.ChurchTools.Models;
using ElvantoSync.Nextcloud;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebDav;
using SyncDbContext = ElvantoSync.Persistence.DbContext;

namespace ElvantoSync.Tests;

public class PeopleToContactAreaTests
{
    [Theory]
    [InlineData(new int[] { }, new string[] { "42", "43", "44" })]
    [InlineData(new int[] { 1 }, new string[] { "42" })]
    [InlineData(new int[] { 3, 5 }, new string[] { "42", "43" })]
    [InlineData(new int[] { 99 }, new string[] { })]
    public async Task IncludesPeopleInAnyContactAreaIndependentlyOfAccountAreas(int[] includedAreas, string[] expectedIds)
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

        var configValues = new Dictionary<string, string?>
        {
            ["Sync:Nextcloud:PeopleToNextcloudSync:IncludedAreaIds:0"] = "99",
        };
        for (int i = 0; i < includedAreas.Length; i++)
        {
            configValues[$"Sync:Nextcloud:PeopleToContactSync:IncludedAreaIds:{i}"] = includedAreas[i].ToString();
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        using var http = new HttpClient();
        using var webDav = new WebDavClient(http);
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddApplicationOptions();
        services.AddLogging();
        services.AddSingleton(source.Object);
        services.AddSingleton(db);
        services.AddSingleton(http);
        services.AddSingleton(webDav);
        using var provider = services.BuildServiceProvider();
        // Resolve the internal sync through its public base without changing
        // production visibility solely for tests.
        var syncType = typeof(PeopleToNextcloudSync).Assembly.GetType("ElvantoSync.Nextcloud.PeopleToNextcloudContactSync", throwOnError: true)!;
        var sync = (Sync<Person, WebDavResource>)ActivatorUtilities.CreateInstance(provider, syncType);

        var result = await sync.GetFromAsync();

        Assert.Equal(expectedIds, result.Select(p => p.Id));
        Assert.Equal(people, await source.Object.GetPeopleAsync());
    }
}
