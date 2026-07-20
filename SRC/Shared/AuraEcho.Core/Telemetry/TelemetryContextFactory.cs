using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Telemetry;

/// <summary>
/// 构建遥测上报时的全局上下文信息。
/// </summary>
public class TelemetryContextFactory
{
    private readonly Lazy<Guid> _sessionId;
    private readonly Lazy<TelemetryContext> _context;

    public TelemetryContextFactory()
    {
        _sessionId = new Lazy<Guid>(Guid.NewGuid);
        _context = new Lazy<TelemetryContext>(BuildContext);
    }

    /// <summary>
    /// 当前会话标识。
    /// </summary>
    public Guid SessionId => _sessionId.Value;

    /// <summary>
    /// 当前会话的设备和环境上下文。
    /// </summary>
    public TelemetryContext Context => _context.Value;

    private static TelemetryContext BuildContext()
    {
        var (screenResolution, screenDpi) = GetScreenInfo();

        return new TelemetryContext
        {
            InstallationId = GetInstallationId(),
            AppVersion = GetAppVersion(),
            OsVersion = RuntimeInformation.OSDescription,
            NetVersion = Environment.Version.ToString(),
            Culture = System.Globalization.CultureInfo.CurrentCulture.Name,
            CpuModel = GetCpuModel(),
            GpuModel = GetGpuModel(),
            ScreenResolution = screenResolution,
            ScreenDpi = screenDpi
        };

        static Guid GetInstallationId()
        {
            var existing = SecureStore.Load(SecureStoreKeys.InstallationId);
            if (Guid.TryParse(existing, out var id))
                return id;

            var newId = Guid.NewGuid();
            SecureStore.Save(SecureStoreKeys.InstallationId, newId.ToString());
            return newId;
        }

        static string GetAppVersion()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    var version = assembly.GetName().Version;
                    if (version != null)
                        return version.ToString();
                }
            }
            catch
            {
                // 忽略版本获取失败
            }

            return "0.0.0.0";
        }
    }

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    /// <summary>
    /// 主屏物理分辨率与系统 DPI
    /// </summary>
    private static (string Resolution, int Dpi) GetScreenInfo()
    {
        try
        {
            var width = GetSystemMetrics(SM_CXSCREEN);
            var height = GetSystemMetrics(SM_CYSCREEN);
            var resolution = width > 0 && height > 0 ? $"{width}x{height}" : string.Empty;

            int dpi;
            try
            {
                dpi = (int)GetDpiForSystem();
            }
            catch
            {
                dpi = 0;
            }

            return (resolution, dpi);
        }
        catch
        {
            return (string.Empty, 0);
        }
    }

    /// <summary>
    /// CPU 型号
    /// </summary>
    private static string GetCpuModel() => QueryWmiString("Win32_Processor", "Name");

    /// <summary>
    /// 显卡型号
    /// </summary>
    private static string GetGpuModel()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, AdapterRAM FROM Win32_VideoController");

            string bestName = string.Empty;
            long bestRam = -1;

            foreach (var obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var ramRaw = obj["AdapterRAM"];
                var ram = ramRaw is long l ? l
                        : ramRaw is uint u ? u
                        : 0;

                // 虚拟适配器显存通常为 0；优先选取显存最大的适配器
                if (ram > bestRam)
                {
                    bestRam = ram;
                    bestName = name;
                }
            }

            if (!string.IsNullOrWhiteSpace(bestName))
                return bestName;
        }
        catch
        {
            // 忽略 WMI 查询失败
        }

        return string.Empty;
    }

    /// <summary>
    /// 查询指定 WMI 类的字符串属性，取首个非空结果。
    /// </summary>
    private static string QueryWmiString(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            foreach (var obj in searcher.Get())
            {
                var value = obj[property]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
        }
        catch
        {
            // 忽略 WMI 查询失败
        }

        return string.Empty;
    }
}
