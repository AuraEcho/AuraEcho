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
        if (!File.Exists(ApplicationPaths.TelemetryDataBase))
        {
            using var db = CreateContext();
            db.Database.Migrate();
        }
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
    /// 取出最多 <paramref name="maxCount"/> 条未发送事件，按创建时间升序。
    /// </summary>
    public List<TelemetryEvent> Dequeue(int maxCount)
    {
        using var db = CreateContext();
        return db.TelemetryEvents
                 .AsNoTracking()
                 .OrderBy(e => e.CreatedAt)
                 .Take(maxCount)
                 .AsEnumerable()
                 .Select(ToEvent)
                 .ToList();
    }

    /// <summary>
    /// 批量删除已成功发送的事件。
    /// </summary>
    public void Delete(IEnumerable<string> ids)
    {
        using var db = CreateContext();
        db.TelemetryEvents
          .Where(e => ids.Contains(e.Id))
          .ExecuteDelete();
    }

    /// <summary>
    /// 递增指定事件的重试次数。
    /// </summary>
    public void IncrementRetryCount(IEnumerable<string> ids)
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

    private static TelemetryEventEntity ToEntity(TelemetryEvent evt) => new()
    {
        Id = evt.Id,
        Timestamp = evt.Timestamp,
        Type = evt.Type.ToString(),
        Name = evt.Name,
        Properties = evt.Properties,
        Metrics = evt.Metrics,
        RetryCount = 0,
        CreatedAt = DateTime.UtcNow
    };

    private static TelemetryEvent ToEvent(TelemetryEventEntity entity) => new()
    {
        Id = entity.Id,
        Timestamp = entity.Timestamp,
        Type = Enum.Parse<TelemetryEventType>(entity.Type),
        Name = entity.Name,
        Properties = entity.Properties,
        Metrics = entity.Metrics
    };
}
