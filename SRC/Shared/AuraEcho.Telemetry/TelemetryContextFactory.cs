using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AuraEcho.Telemetry;

/// <summary>
/// 构建遥测上报时的全局上下文信息。
/// InstallationId 通过 <see cref="_installationIdProvider"/> 由调用方注入，
/// 避免对 SecureStore 等持久化组件的直接依赖。
/// </summary>
public class TelemetryContextFactory
{
    private readonly Func<Guid> _installationIdProvider;
    private readonly Lazy<Guid> _sessionId;
    private readonly Lazy<TelemetryContext> _context;

    public TelemetryContextFactory(Func<Guid> installationIdProvider)
    {
        _installationIdProvider = installationIdProvider ?? throw new ArgumentNullException(nameof(installationIdProvider));
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

    private TelemetryContext BuildContext()
    {
        var (screenResolution, screenDpi) = GetScreenInfo();

        return new TelemetryContext
        {
            InstallationId = _installationIdProvider(),
            AppVersion = GetAppVersion(),
            OsVersion = RuntimeInformation.OSDescription,
            NetVersion = Environment.Version.ToString(),
            Culture = System.Globalization.CultureInfo.CurrentCulture.Name,
            CpuModel = GetCpuModel(),
            GpuModel = GetGpuModel(),
            ScreenResolution = screenResolution,
            ScreenDpi = screenDpi,
            NetworkType = GetNetworkType()
        };
    }

    private static string GetAppVersion()
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
    /// 启动时的主要网络连接类型。
    /// 在处于 Up 状态、非回环/隧道的接口中，优先返回无线，其次有线，均无则 Unknown；无活跃接口返回 None。
    /// </summary>
    private static string GetNetworkType()
    {
        try
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return "None";

            var hasWired = false;
            var hasUnknown = false;

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                switch (ni.NetworkInterfaceType)
                {
                    case NetworkInterfaceType.Loopback:
                    case NetworkInterfaceType.Tunnel:
                        continue;
                    case NetworkInterfaceType.Wireless80211:
                        // 无线优先，直接返回
                        return "Wireless";
                    case NetworkInterfaceType.Ethernet:
                    case NetworkInterfaceType.GigabitEthernet:
                    case NetworkInterfaceType.FastEthernetT:
                    case NetworkInterfaceType.FastEthernetFx:
                        hasWired = true;
                        break;
                    default:
                        hasUnknown = true;
                        break;
                }
            }

            if (hasWired) return "Wired";
            if (hasUnknown) return "Unknown";
            return "None";
        }
        catch
        {
            return string.Empty;
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
