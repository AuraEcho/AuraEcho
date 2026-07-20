using System.Text.Json;
using AuraEcho.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AuraEcho.Core.Data;

/// <summary>
/// 遥测数据本地缓存上下文。
/// </summary>
public class TelemetryDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DbSet<TelemetryEventEntity> TelemetryEvents { get; set; }

    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stringDictConverter = new ValueConverter<Dictionary<string, string>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
            v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions));

        var doubleDictConverter = new ValueConverter<Dictionary<string, double>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
            v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<Dictionary<string, double>>(v, JsonOptions));

        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        modelBuilder.Entity<TelemetryEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                  .IsRequired();

            entity.Property(e => e.Timestamp)
                  .HasConversion(utcConverter);

            entity.Property(e => e.CreatedAt)
                  .HasConversion(utcConverter);

            entity.Property(e => e.Properties)
                  .HasConversion(stringDictConverter);
            entity.Property(e => e.Properties).Metadata.SetValueComparer(
                new ValueComparer<Dictionary<string, string>?>(favorStructuralComparisons: true));

            entity.Property(e => e.Metrics)
                  .HasConversion(doubleDictConverter);
            entity.Property(e => e.Metrics).Metadata.SetValueComparer(
                new ValueComparer<Dictionary<string, double>?>(favorStructuralComparisons: true));

            entity.HasIndex(e => e.CreatedAt);

            entity.HasIndex(e => new { e.SessionId, e.SequenceNumber });
        });
    }
}
