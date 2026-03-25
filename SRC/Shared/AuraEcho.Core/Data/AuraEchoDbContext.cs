using Microsoft.EntityFrameworkCore;
using AuraEcho.Core.Data.Entities;

namespace AuraEcho.Core.Data;

public class AuraEchoDbContext : DbContext
{
    public DbSet<LocalPlugin> LocalPlugins { get; set; }
    public DbSet<UserPlugin> UserPlugins { get; set; }

    public AuraEchoDbContext(DbContextOptions<AuraEchoDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LocalPlugin>(pr => pr.OwnsOne(p => p.Manifest));
    }
}
