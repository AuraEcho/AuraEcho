using Prism.Mvvm;

namespace AuraEcho.Models;

/// <summary>
/// 等待重启的项
/// </summary>
public class PendingRestartItem : BindableBase
{
    public string Name
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string Description
    {
        get;
        set => SetProperty(ref field, value);
    }
}
