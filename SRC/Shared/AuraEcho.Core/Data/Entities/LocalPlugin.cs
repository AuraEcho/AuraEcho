using AuraEcho.Core.Models;
using Prism.Mvvm;

namespace AuraEcho.Core.Data.Entities;

/// <summary>
/// 模块配置信息
/// </summary>
public class LocalPlugin : BindableBase
{
    public Guid Id { get; set; }

    /// <summary>
    /// 清单信息
    /// </summary>
    public PluginManifest Manifest
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 模块所在目录路径
    /// </summary>
    public string PluginFolder { get; set; } = String.Empty;

    public bool IsSetup { get; set; }
}
