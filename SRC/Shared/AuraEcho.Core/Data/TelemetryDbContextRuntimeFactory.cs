using Microsoft.EntityFrameworkCore;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Data;

public static class TelemetryDbContextRuntimeFactory
{
    private static readonly DbContextOptions<TelemetryDbContext> _options;

    static TelemetryDbContextRuntimeFactory()
    {
        _options =
            new DbContextOptionsBuilder<TelemetryDbContext>()
                .UseSqlite($"Data Source={ApplicationPaths.TelemetryDataBase}")
                .Options;
    }

    public static TelemetryDbContext CreateDbContext()
    {
        return new TelemetryDbContext(_options);
    }
}
