using System.IO;
using AuraEcho.Cloud.V1.Models.Telemetry;
using AuraEcho.Core.Data;
using AuraEcho.Core.Data.Entities;
using AuraEcho.Core.Tools;
using Microsoft.EntityFrameworkCore;

namespace AuraEcho.Core.Telemetry;

/// <summary>
/// 遥测数据本地缓存
/// </summary>
public class TelemetryStore
{
    private readonly TelemetryContextFactory _contextFactory;

    public TelemetryStore(TelemetryContextFactory contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        if (File.Exists(ApplicationPaths.TelemetryDataBase)) return;

        using var db = CreateContext();
        db.Database.Migrate();
    }

    private static TelemetryDbContext CreateContext() => TelemetryDbContextRuntimeFactory.CreateDbContext();

    /// <summary>
    /// 写入单条遥测事件。
    /// </summary>
    public void Enqueue(TelemetryEvent evt)
    {
        using var db = CreateContext();
        db.TelemetryEvents.Add(ToEntity(evt, _contextFactory.Context));
        db.SaveChanges();
    }

    /// <summary>
    /// 取出最多 <paramref name="maxCount"/> 条未发送事件。
    /// </summary>
    public List<TelemetryEventRecord> Dequeue(int maxCount)
    {
        using var db = CreateContext();
        return db.TelemetryEvents
                 .AsNoTracking()
                 .OrderBy(e => e.CreatedAt)
                 .Take(maxCount)
                 .AsEnumerable()
                 .Select(ToRecord)
                 .ToList();
    }

    /// <summary>
    /// 批量删除已成功发送的事件。
    /// </summary>
    public void Delete(IEnumerable<Guid> ids)
    {
        using var db = CreateContext();
        db.TelemetryEvents
          .Where(e => ids.Contains(e.Id))
          .ExecuteDelete();
    }

    /// <summary>
    /// 递增指定事件的重试次数。
    /// </summary>
    public void IncrementRetryCount(IEnumerable<Guid> ids)
    {
        using var db = CreateContext();
        db.TelemetryEvents
          .Where(e => ids.Contains(e.Id))
          .ExecuteUpdate(setters => setters.SetProperty(e => e.RetryCount, e => e.RetryCount + 1));
    }

    /// <summary>
    /// 删除重试次数超过上限的废弃事件。
    /// </summary>
    public int PurgeDeadEvents(int maxRetries)
    {
        using var db = CreateContext();
        return db.TelemetryEvents
                 .Where(e => e.RetryCount >= maxRetries)
                 .ExecuteDelete();
    }

    /// <summary>
    /// 获取待发送事件总数。
    /// </summary>
    public int GetPendingCount()
    {
        using var db = CreateContext();
        return db.TelemetryEvents.Count();
    }

    private static TelemetryEventEntity ToEntity(TelemetryEvent evt, TelemetryContext context) => new()
    {
        Id = evt.Id,
        Timestamp = evt.Timestamp,
        Type = evt.Type,
        Name = evt.Name,
        Properties = evt.Properties,
        Metrics = evt.Metrics,
        Culture = evt.Culture,
        InstallationId = context.InstallationId,
        AppVersion = context.AppVersion,
        OSVersion = context.OSVersion,
        NetVersion = context.NetVersion,
        SessionId = context.SessionId,
        RetryCount = 0,
        CreatedAt = DateTime.UtcNow
    };

    private static TelemetryEventRecord ToRecord(TelemetryEventEntity entity) => new()
    {
        Id = entity.Id,
        Timestamp = entity.Timestamp,
        Type = entity.Type,
        Name = entity.Name,
        Culture = entity.Culture,
        Properties = entity.Properties,
        Metrics = entity.Metrics,
        InstallationId = entity.InstallationId,
        AppVersion = entity.AppVersion,
        OSVersion = entity.OSVersion,
        NetVersion = entity.NetVersion,
        SessionId = entity.SessionId
    };
}
