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
    public TelemetryStore()
    {
#if !DEBUG
        // 发布版本会在安装阶段完成数据库迁移
        if (File.Exists(ApplicationPaths.TelemetryDataBase)) return;
#endif

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
        db.TelemetryEvents.Add(ToEntity(evt));
        db.SaveChanges();
    }

    /// <summary>
    /// 批量写入遥测事件
    /// </summary>
    public void EnqueueBatch(IReadOnlyList<TelemetryEvent> events)
    {
        if (events.Count == 0) return;

        using var db = CreateContext();
        foreach (var evt in events)
        {
            db.TelemetryEvents.Add(ToEntity(evt));
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

    private static TelemetryEventEntity ToEntity(TelemetryEvent evt) => new()
    {
        Id = evt.Id,
        Timestamp = evt.Timestamp,
        Type = evt.Type,
        Name = evt.Name,
        Properties = evt.Properties,
        Metrics = evt.Metrics,
        SessionId = evt.SessionId,
        CreatedAt = DateTime.UtcNow
    };

    private static TelemetryEventRecord ToRecord(TelemetryEventEntity entity) => new()
    {
        Id = entity.Id,
        Timestamp = entity.Timestamp,
        Type = entity.Type,
        Name = entity.Name,
        Properties = entity.Properties,
        Metrics = entity.Metrics,
        SessionId = entity.SessionId
    };
}
