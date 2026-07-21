using Serilog.Events;

namespace AuraEcho.Core.Logging;

/// <summary>
/// 日志后端构建选项。四个进程（宿主、Launcher、Updater、Telemetry）共用同一套后端配置，
/// 仅通过本选项区分各自的输出目录、文件名前缀与进程标识。
/// </summary>
public sealed class LoggingOptions
{
    /// <param name="logDirectory">日志文件输出目录。</param>
    /// <param name="fileNamePrefix">日志文件名前缀。</param>
    /// <param name="processName">进程标识。</param>
    public LoggingOptions(string logDirectory, string fileNamePrefix, string processName)
    {
        LogDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
        FileNamePrefix = fileNamePrefix ?? throw new ArgumentNullException(nameof(fileNamePrefix));
        ProcessName = processName ?? throw new ArgumentNullException(nameof(processName));
    }

    /// <summary>
    /// 日志文件输出目录。
    /// </summary>
    public string LogDirectory { get; }

    /// <summary>
    /// 日志文件名前缀。
    /// </summary>
    public string FileNamePrefix { get; }

    /// <summary>
    /// 进程标识
    /// </summary>
    public string ProcessName { get; }

    /// <summary>
    /// 最低日志级别。
    /// </summary>
    public LogEventLevel MinimumLevel { get; init; } =
#if DEBUG
        LogEventLevel.Debug;
#else
        LogEventLevel.Information;
#endif

    /// <summary>
    /// 保留的滚动日志文件数量（按天滚动，等价于保留天数）。
    /// </summary>
    public int RetainedFileCountLimit { get; init; } = 14;

    /// <summary>
    /// 单个日志文件大小上限（字节），超出后自动切分。默认 20 MB。
    /// </summary>
    public long FileSizeLimitBytes { get; init; } = 20 * 1024 * 1024;
}
