using ElvantoSync.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElvantoSync.Persistence;

public class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public virtual DbSet<IndexMapping> IndexMappings { get; set; }

    public DbContext(DbContextOptions<DbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IndexMapping>()
            .HasKey(i => new { i.FromId, i.ToId, i.Type });

        modelBuilder.Entity<IndexMapping>()
            .HasIndex(i => new { i.FromId, i.Type });
    }
}
