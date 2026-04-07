using AuraEcho.PluginContracts.Constants;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class ImageViewerViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    private readonly IRegionManager _regionManager;

    public string ImageFilePath
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand CloseCommand { get; }
    public void Close()
    {
        _regionManager.Regions[HostRegionNames.ContentDialogRegion].RemoveAll();
    }

    public ImageViewerViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;

        CloseCommand = new DelegateCommand(Close);
    }

    public bool KeepAlive => false;

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        ImageFilePath = navigationContext.Parameters.GetValue<string>("ImageFilePath");
    }
}
