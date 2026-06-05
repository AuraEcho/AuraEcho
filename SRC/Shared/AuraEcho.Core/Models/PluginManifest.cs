using AuraEcho.Api.Models.V1.Plugin;
using Prism.Mvvm;

namespace AuraEcho.Core.Models;

/// <summary>
/// 扩展模块清单
/// </summary>
public class PluginManifest : BindableBase
{
    public PluginType Type
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Id
    /// </summary>
    public Guid Id  
    { 
        get; 
        set => SetProperty(ref field, value); 
    }
    
    /// <summary>
    /// 图标文件名称(icon.png)
    /// </summary>
    public string? Icon
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 作者
    /// </summary>
    public string? Author
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string? PluginName 
    { 
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 版本
    /// </summary>
    public string? Version 
    { 
        get; 
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// 入口程序集
    /// </summary>
    public string? EntryFileName 
    { 
        get; 
        set => SetProperty(ref field, value);
    }
}
