using AuraEcho.PluginContracts.Interfaces;
using Prism.Mvvm;

namespace AuraEcho.Core.Models;

public class UserPluginModel : BindableBase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public LocalPluginModel? LocalPlugin { get; set; }

    public PluginPlanStatus Status 
    { 
        get;
        set => SetProperty(ref field, value); 
    }

    public IPlugin PluginContext { get; set; }
}
