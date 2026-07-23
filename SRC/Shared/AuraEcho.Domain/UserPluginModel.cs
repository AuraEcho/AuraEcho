using Prism.Mvvm;

namespace AuraEcho.Domain;

public class UserPluginModel : BindableBase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public InstalledPluginModel? LocalPlugin { get; set; }

    public PluginPlanStatus Status
    {
        get;
        set => SetProperty(ref field, value);
    }
}
