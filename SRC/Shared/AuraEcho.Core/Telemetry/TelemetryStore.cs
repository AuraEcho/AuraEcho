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

        // 始终调用 Migrate()：新建库会完整建表，已有库补执行未应用的迁移（如设备画像列）。
        // Migrate() 是幂等的，不会重复执行已应用的迁移。
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
    /// 批量写入遥测事件
    /// </summary>
    public void EnqueueBatch(IReadOnlyList<TelemetryEvent> events)
    {
        if (events.Count == 0) return;

        var context = _contextFactory.Context;
        using var db = CreateContext();
        foreach (var evt in events)
        {
            db.TelemetryEvents.Add(ToEntity(evt, context));
        }
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
    /// 裁剪本地缓存，当总条数超过上限时，删除最旧的溢出部分。
    /// </summary>
    /// <returns>被删除的事件数。</returns>
    public int TrimToCapacity(int maxEvents)
    {
        if (maxEvents <= 0) return 0;

        using var db = CreateContext();

        var total = db.TelemetryEvents.Count();
        var overflow = total - maxEvents;
        if (overflow <= 0) return 0;

        var staleIds = db.TelemetryEvents
                         .OrderBy(e => e.CreatedAt)
                         .Take(overflow)
                         .Select(e => e.Id);

        return db.TelemetryEvents
                 .Where(e => staleIds.Contains(e.Id))
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
        CpuModel = context.CpuModel,
        CpuCoreCount = context.CpuCoreCount,
        GpuModel = context.GpuModel,
        ScreenResolution = context.ScreenResolution,
        ScreenDpi = context.ScreenDpi,
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
        SessionId = entity.SessionId,
        CpuModel = entity.CpuModel,
        CpuCoreCount = entity.CpuCoreCount,
        GpuModel = entity.GpuModel,
        ScreenResolution = entity.ScreenResolution,
        ScreenDpi = entity.ScreenDpi
    };
}
