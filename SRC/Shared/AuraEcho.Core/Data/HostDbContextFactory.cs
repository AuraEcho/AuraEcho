using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Data;

public class HostDbContextFactory : IDesignTimeDbContextFactory<HostDbContext>
{
    public HostDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HostDbContext>();

        optionsBuilder.UseSqlite($"Data Source={ApplicationPaths.HostDataBase}");
        return new HostDbContext(optionsBuilder.Options);
    }
}
