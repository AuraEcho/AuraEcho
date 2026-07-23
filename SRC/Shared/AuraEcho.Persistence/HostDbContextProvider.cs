using Microsoft.EntityFrameworkCore;

namespace AuraEcho.Persistence;

/// <summary>
/// 运行时 HostDbContext 提供者。
/// </summary>
public class HostDbContextProvider
{
    private readonly DbContextOptions<HostDbContext> _options;

    public HostDbContextProvider(string dbPath)
    {
        _options =
            new DbContextOptionsBuilder<HostDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
    }

    public HostDbContext CreateDbContext()
    {
        return new HostDbContext(_options);
    }
}
