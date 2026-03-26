namespace AuraEcho.Core.Models;

public class LocalPluginModel
{
    public Guid Id { get; set; }

    /// <summary>
    /// 清单信息
    /// </summary>
    public PluginManifest Manifest { get; set; }

    /// <summary>
    /// 模块所在目录路径
    /// </summary>
    public string PluginFolder { get; set; } = String.Empty;

    public bool IsSetup { get; set; }
}
