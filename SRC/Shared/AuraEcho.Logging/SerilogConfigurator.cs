using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace AuraEcho.Logging;

/// <summary>
/// 统一的 Serilog 构建器。
/// </summary>
public static class SerilogConfigurator
{
    private const string OutputTemplate =
        "{Timestamp:HH:mm:ss.fff} [{Level:u3}] ({Process}) {Message:lj}{NewLine}{Exception}";

    private const string DebugPrefix = "[AURAECHO]";

    /// <summary>
    /// 根据选项构建一个独立的 Serilog <see cref="Logger"/>。
    /// </summary>
    /// <param name="options">日志后端选项。</param>
    /// <param name="levelSwitch">输出的级别开关，可在运行时调整 <see cref="LoggingLevelSwitch.MinimumLevel"/> 动态改变日志级别。</param>
    public static Logger CreateLogger(LoggingOptions options, out LoggingLevelSwitch levelSwitch)
    {
        ArgumentNullException.ThrowIfNull(options);

        Directory.CreateDirectory(options.LogDirectory);

        levelSwitch = new LoggingLevelSwitch(options.MinimumLevel);

        var configuration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Process", options.ProcessName)
            .WriteTo.Async(a => a.File(
                path: Path.Combine(options.LogDirectory, $"{options.FileNamePrefix}.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                fileSizeLimitBytes: options.FileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                shared: true,
                outputTemplate: OutputTemplate));

#if DEBUG
        configuration = configuration.WriteTo.Debug(
            outputTemplate: $"{DebugPrefix} {OutputTemplate}");
#endif

        return configuration.CreateLogger();
    }

    /// <summary>
    /// 构建一个由本类统一配置的 <see cref="ILoggerFactory"/>。
    /// 供无 Hosting 的项目手动接入 MEL 使用。
    /// </summary>
    public static ILoggerFactory CreateLoggerFactory(LoggingOptions options, out LoggingLevelSwitch levelSwitch)
    {
        var logger = CreateLogger(options, out levelSwitch);
        // dispose: true —— 工厂释放时一并释放底层 Serilog logger（会 flush 异步 sink）。
        return new SerilogLoggerFactory(logger, dispose: true);
    }

    /// <summary>
    /// 将统一的 Serilog 后端接入 <see cref="ILoggingBuilder"/>。
    /// 供使用 Hosting 的 F# 服务在 <c>ConfigureLogging</c> 中调用。
    /// </summary>
    public static ILoggingBuilder AddAuraEchoSerilog(this ILoggingBuilder builder, LoggingOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var logger = CreateLogger(options, out _);

        builder.ClearProviders();
        builder.AddProvider(new SerilogLoggerProvider(logger, dispose: true));
        return builder;
    }
}
