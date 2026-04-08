using PluginInstaller.Constants;
using AuraEcho.Core.Contracts;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace PluginInstaller.ViewModels;

public class PickPluginInstallFileViewModel : BindableBase, IRegionMemberLifetime
{
    #region private members
    private readonly IRegionManager _regionManager;
    private readonly IFileDialogService _fileDialogService;
    #endregion

    public DelegateCommand<string> NavigationToInstallPreparationCommand { get; }
    private void NavigationToInstallPreparation(string pluginInstallFile)
    {
        _regionManager.RequestNavigate(
            RegionNames.MainRegion,
            ViewNames.InstallPreparation,
            new NavigationParameters
            {
                { "PluginInstallFilePath", pluginInstallFile }
            });
    }

    public DelegateCommand PickPluginInstallFileCommand { get; }
    private void PickPluginInstallFile()
    {
        var filePath = _fileDialogService.OpenFile("选择 AuraEcho 模块安装文件", "AuraEcho 模块安装文件|*.aex");
        if (filePath is null) return;

        NavigationToInstallPreparation(filePath);
    }

    public PickPluginInstallFileViewModel(IRegionManager regionManager, IFileDialogService fileDialogService)
    {
        _regionManager = regionManager;
        _fileDialogService = fileDialogService;

        NavigationToInstallPreparationCommand = new DelegateCommand<string>(NavigationToInstallPreparation);
        PickPluginInstallFileCommand = new DelegateCommand(PickPluginInstallFile);
    }

    public bool KeepAlive => false;
}
