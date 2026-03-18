using Prism.Mvvm;

namespace AuraEcho.PluginContracts.Models;

public class AuraToast : BindableBase
{
    public string Message { get; init; }
    public bool IsClosing 
    { 
        get;
        set => SetProperty(ref field, value);
    }
    public ToastLevel Level { get; init; }
}
