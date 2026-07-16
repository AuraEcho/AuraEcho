using Microsoft.EntityFrameworkCore;
using AuraEcho.Core.Data.Entities;

namespace AuraEcho.Core.Data;

public class HostDbContext : DbContext
{
    public DbSet<InstalledPlugin> InstalledPlugin { get; set; }
    public DbSet<UserPlugin> UserPlugin { get; set; }

    public HostDbContext(DbContextOptions<HostDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}
