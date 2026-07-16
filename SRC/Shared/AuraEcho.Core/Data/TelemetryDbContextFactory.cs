using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Data;

public class TelemetryDbContextFactory : IDesignTimeDbContextFactory<TelemetryDbContext>
{
    public TelemetryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TelemetryDbContext>();

        optionsBuilder.UseSqlite($"Data Source={ApplicationPaths.TelemetryDataBase}");
        return new TelemetryDbContext(optionsBuilder.Options);
    }
}
