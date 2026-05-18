using Prism.Mvvm;
using System;
namespace AuraEcho.Models;

/// <summary>
/// 等待重启的项
/// </summary>
public class PendingRestartItem : BindableBase
{
    public Guid Id
    {
        get;
        set => SetProperty(ref field, value);
    } = Guid.NewGuid();

    public string Title
    {
        get;
        set => SetProperty(ref field, value);
    }
}
