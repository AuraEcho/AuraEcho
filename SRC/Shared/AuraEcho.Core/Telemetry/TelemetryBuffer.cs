using System.Text.Json;
using AuraEcho.Cloud.V1.Models.Telemetry;
using AuraEcho.Core.Tools;
using Microsoft.Data.Sqlite;

namespace AuraEcho.Core.Telemetry;

/// <summary>
/// 遥测数据本地缓存
/// </summary>
public class TelemetryBuffer : IDisposable
{
    private readonly SqliteConnection _connection;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TelemetryBuffer()
    {
        _connection = new SqliteConnection($"Data Source={ApplicationPaths.TelemetryDataBase}");
        _connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS TelemetryEvents (
                Id          TEXT PRIMARY KEY,
                Timestamp   TEXT NOT NULL,
                Type        TEXT NOT NULL,
                Name        TEXT NOT NULL,
                Payload     TEXT NOT NULL,
                RetryCount  INTEGER DEFAULT 0,
                CreatedAt   TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_TelemetryEvents_CreatedAt ON TelemetryEvents(CreatedAt);
            """;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 写入单条遥测事件。
    /// </summary>
    public void Enqueue(TelemetryEvent evt)
    {
        const string sql = """
            INSERT INTO TelemetryEvents (Id, Timestamp, Type, Name, Payload, RetryCount, CreatedAt)
            VALUES (@Id, @Timestamp, @Type, @Name, @Payload, 0, @CreatedAt);
            """;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", evt.Id);
        cmd.Parameters.AddWithValue("@Timestamp", evt.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("@Type", evt.Type.ToString());
        cmd.Parameters.AddWithValue("@Name", evt.Name);
        cmd.Parameters.AddWithValue("@Payload", SerializeEvent(evt));
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 取出最多 <paramref name="maxCount"/> 条未发送事件，按创建时间升序。
    /// </summary>
    public List<TelemetryEvent> Dequeue(int maxCount)
    {
        const string sql = """
            SELECT Id, Timestamp, Type, Name, Payload, RetryCount
            FROM TelemetryEvents
            ORDER BY CreatedAt ASC
            LIMIT @MaxCount;
            """;

        var events = new List<TelemetryEvent>(maxCount);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@MaxCount", maxCount);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var evt = DeserializeEvent(
                reader.GetString(3),  // Name
                reader.GetString(2),  // Type
                reader.GetString(4)   // Payload
            );
            if (evt != null)
            {
                evt.Id = reader.GetString(0);
                evt.Timestamp = DateTime.Parse(reader.GetString(1));
                evt.Type = Enum.Parse<TelemetryEventType>(reader.GetString(2));
                evt.Name = reader.GetString(3);
                events.Add(evt);
            }
        }

        return events;
    }

    /// <summary>
    /// 批量删除已成功发送的事件。
    /// </summary>
    public void Delete(IEnumerable<string> ids)
    {
        using var transaction = _connection.BeginTransaction();
        foreach (var id in ids)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM TelemetryEvents WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    /// <summary>
    /// 递增指定事件的重试次数。
    /// </summary>
    public void IncrementRetryCount(IEnumerable<string> ids)
    {
        using var transaction = _connection.BeginTransaction();
        foreach (var id in ids)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE TelemetryEvents SET RetryCount = RetryCount + 1 WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    /// <summary>
    /// 删除重试次数超过上限的废弃事件。
    /// </summary>
    public int PurgeDeadEvents(int maxRetries)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM TelemetryEvents WHERE RetryCount >= @MaxRetries;";
        cmd.Parameters.AddWithValue("@MaxRetries", maxRetries);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 获取待发送事件总数。
    /// </summary>
    public int GetPendingCount()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM TelemetryEvents;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static string SerializeEvent(TelemetryEvent evt)
    {
        return JsonSerializer.Serialize(new
        {
            properties = evt.Properties,
            metrics = evt.Metrics
        }, JsonOptions);
    }

    private static TelemetryEvent? DeserializeEvent(string name, string type, string payload)
    {
        try
        {
            var wrapper = JsonSerializer.Deserialize<EventPayload>(payload, JsonOptions);
            if (wrapper == null) return null;

            return new TelemetryEvent
            {
                Name = name,
                Type = Enum.Parse<TelemetryEventType>(type),
                Properties = wrapper.Properties,
                Metrics = wrapper.Metrics
            };
        }
        catch
        {
            return null;
        }
    }

    private class EventPayload
    {
        public Dictionary<string, string>? Properties { get; set; }
        public Dictionary<string, double>? Metrics { get; set; }
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
