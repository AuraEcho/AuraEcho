using Microsoft.EntityFrameworkCore;
using AuraEcho.Core.Data.Entities;

namespace AuraEcho.Core.Data;

public class AuraEchoDbContext : DbContext
{
    public DbSet<InstalledPlugin> InstalledPlugin { get; set; }
    public DbSet<UserPlugin> UserPlugin { get; set; }

    public AuraEchoDbContext(DbContextOptions<AuraEchoDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}
