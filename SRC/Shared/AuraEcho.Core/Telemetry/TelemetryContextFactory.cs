using System.Reflection;
using System.Runtime.InteropServices;
using AuraEcho.Cloud.V1.Models.Telemetry;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Telemetry;

/// <summary>
/// 构建遥测上报时的全局上下文信息。
/// </summary>
public class TelemetryContextFactory
{
    private readonly Lazy<TelemetryContext> _context;

    public TelemetryContextFactory()
    {
        _context = new Lazy<TelemetryContext>(BuildContext);
    }

    public TelemetryContext Context => _context.Value;

    private static TelemetryContext BuildContext()
    {
        return new TelemetryContext
        {
            InstallationId = GetInstallationId(),
            AppVersion = GetAppVersion(),
            OSVersion = RuntimeInformation.OSDescription,
            NetVersion = Environment.Version.ToString(),
            SessionId = Guid.NewGuid()
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
}
