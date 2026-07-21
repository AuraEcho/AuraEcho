using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuraEcho.Telemetry;

/// <summary>
/// 设计时工厂 —— 供 EF Core 迁移工具使用。
/// 连接字符串可通过环境变量 TELEMETRY_DB_PATH 覆盖。
/// </summary>
public class TelemetryDbContextFactory : IDesignTimeDbContextFactory<TelemetryDbContext>
{
    public TelemetryDbContext CreateDbContext(string[] args)
    {
        var dbPath = Environment.GetEnvironmentVariable("TELEMETRY_DB_PATH")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "AuraEcho", "Client", "Data", "telemetry.db");

        var optionsBuilder = new DbContextOptionsBuilder<TelemetryDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new TelemetryDbContext(optionsBuilder.Options);
    }
}
