using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class WebContainerViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    public string SourceUri
    {
        get;
        set => SetProperty(ref field, value);
    }
    public bool KeepAlive => false;

    public WebContainerViewModel()
    {

    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        SourceUri = navigationContext.Parameters.GetValue<string>("SourceUri");
    }
}
