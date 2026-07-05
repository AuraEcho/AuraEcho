using System.Windows;
using AuraEcho.Cloud.V1.Models.Plugin;
using AuraEcho.PluginContracts.Models;
using Prism.Mvvm;

namespace AuraEcho.Core.Models;

public abstract class AppPlugin : BindableBase
{
    public PluginType PluginType
    {
        get;
        set => SetProperty(ref field, value);
    }

    public PluginPlanStatus PlanStatus
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string WorkingDirectory
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Id
    /// </summary>
    public Guid PluginId
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 图标文件名称(icon.png)
    /// </summary>
    public string? Icon
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 作者
    /// </summary>
    public string? Author
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string? PluginName
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 版本
    /// </summary>
    public string? Version
    {
        get;
        set => SetProperty(ref field, value);
    }

    public abstract ResourceDictionary? GetThemeResource(AppTheme theme);

    protected AppPlugin(PluginManifest manifest)
    {
        PluginId = manifest.Id;
        Icon = manifest.Icon;
        Author = manifest.Author;
        PluginName = manifest.PluginName;
        Version = manifest.Version;
        PluginType = manifest.Type;
    }
}
