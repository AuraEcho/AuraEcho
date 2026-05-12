using PluginPacker.Constants;
using PluginPacker.Views;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Repositories;
using AuraEcho.Core.Services;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.UIToolkit.RegionDialog;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;
using System.Windows;
using AuraEcho.Core.Tools;

namespace PluginPacker;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication
{
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<Homepage>();

        containerRegistry.RegisterInstance<IAppLogger>(new Serilogger(ApplicationPaths.Logs));
        containerRegistry.RegisterSingleton<IFileDialogService, FileDialogService>();
        containerRegistry.RegisterSingleton<IRegionDialogService, RegionDialogService>();

        containerRegistry.RegisterSingleton<IStorageRepository, AlibabaCloudOssRepository>();
        containerRegistry.RegisterSingleton<IRemotePluginRepository, RemotePluginRepository>();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var regionManager = Container.Resolve<IRegionManager>();
        regionManager.RegisterViewWithRegion(RegionNames.MainRegion, ViewNames.Homepage);
    }
}
