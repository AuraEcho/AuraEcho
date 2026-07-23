using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuraEcho.Persistence;

/// <summary>
/// 设计时工厂 —— 供 EF Core 迁移工具使用。
/// 连接字符串可通过环境变量 HOST_DB_PATH 覆盖。
/// </summary>
public class HostDbContextFactory : IDesignTimeDbContextFactory<HostDbContext>
{
    public HostDbContext CreateDbContext(string[] args)
    {
        var dbPath = Environment.GetEnvironmentVariable("HOST_DB_PATH")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "AuraEcho", "Client", "Data", "host.db");

        var optionsBuilder = new DbContextOptionsBuilder<HostDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new HostDbContext(optionsBuilder.Options);
    }
}
