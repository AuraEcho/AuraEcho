using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Data;

public class AuraEchoDbContextFactory : IDesignTimeDbContextFactory<AuraEchoDbContext>
{
    public AuraEchoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuraEchoDbContext>();

        optionsBuilder.UseSqlite($"Data Source={ApplicationPaths.HostDataBase}");
        return new AuraEchoDbContext(optionsBuilder.Options);
    }
}
