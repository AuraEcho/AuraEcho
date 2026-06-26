using AuraEcho.Api.Models.V1.Plugin;

namespace AuraEcho.Core.Data.Entities;

/// <summary>
/// 插件安装信息
/// </summary>
public class InstalledPlugin
{
    public Guid Id { get; set; }

    public Guid PluginId { get; set; }
    
    public PluginType PluginType { get; set; }

    public string? InstallPath { get; set; }

    public DateTime InstaledAt { get; set; }

    public string Version { get; set; }

    public bool IsSetup { get; set; }
}
