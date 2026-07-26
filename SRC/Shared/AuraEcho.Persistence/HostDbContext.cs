using Microsoft.EntityFrameworkCore;
using AuraEcho.Persistence.Entities;

namespace AuraEcho.Persistence;

public class HostDbContext : DbContext
{
    public DbSet<InstalledPlugin> InstalledPlugin { get; set; }
    public DbSet<UserPlugin> UserPlugin { get; set; }
    public DbSet<UserReadAnnouncement> UserReadAnnouncement { get; set; }

    public HostDbContext(DbContextOptions<HostDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserReadAnnouncement>()
                    .HasIndex(r => new { r.UserId, r.AnnouncementId })
                    .IsUnique();
    }
}
