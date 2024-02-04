
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElvantoSync.Tests.UnitTests;

public class IndexMappingRepositoryTest
{
    private readonly DbContext db;

    public IndexMappingRepositoryTest()
    {
        IServiceProvider services = new ServiceCollection()
            .AddDbContext<DbContext>(options => options.UseInMemoryDatabase("testMyContext"))
            .BuildServiceProvider();

        db = services.GetRequiredService<DbContext>();
        db.Database.EnsureCreated();
    }
}
