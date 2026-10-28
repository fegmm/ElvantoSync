using ElvantoSync.Nextcloud;
using ElvantoSync.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ElvantoSync.Persistence;

public class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    private static object DatabaseCreationLock = new object();
    
    public virtual DbSet<IndexMapping> IndexMappings { get; set; }

    public DbContext(DbContextOptions<DbContext> options) : base(options) {
        lock (DatabaseCreationLock) {
            Database.EnsureCreated();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IndexMapping>()
            .HasKey(i => new { i.FromId, i.ToId, i.Type });

        modelBuilder.Entity<IndexMapping>()
            .HasIndex(i => new { i.FromId, i.Type });
    }
    
    public string ChurchToolsToNextcloudGroupId(string churchToolsId)
        => IndexMappings
            .FirstOrDefault(i => i.FromId == churchToolsId && i.Type == nameof(GroupsToNextcloudSync))
            ?.ToId;

    public string ChurchToolsToNextcloudPeopleId(string churchToolsId)
        => IndexMappings
            .FirstOrDefault(i => i.FromId == churchToolsId && i.Type == nameof(PeopleToNextcloudSync))
            ?.ToId;
}
