using System.IO;
using System.IO.Compression;
using System.Text.Json;
using PluginInstaller.Constants;
using AuraEcho.Core.Models;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using AuraEcho.Core.Tools;

namespace PluginInstaller.ViewModels;

/// <summary>
/// 安装准备
/// </summary>
public class InstallPreparationViewModel : BindableBase, INavigationAware
{
    #region private members
    private readonly IAppLogger? _logger;
    private readonly IRegionManager? _regionManager;

    private PluginManifest? _pluginManifest;
    private string _tempExtractPath;
    private const string MANIFEST_FILE_NAME = "plugin.manifest.json";
    private string _pluginInstallFilePath;
    #endregion

    /// <summary>
    /// 模块清单信息
    /// </summary>
    public PluginManifest? PluginManifest
    {
        get => _pluginManifest;
        set => SetProperty(ref _pluginManifest, value);
    }

    public DelegateCommand LoadPluginCommand { get; }
    /// <summary>
    /// 加载模块清单
    /// </summary>
    /// <exception cref="FileNotFoundException"></exception>
    private void LoadPlugin()
    {
        // 解压插件到临时目录
        _tempExtractPath = Path.Combine(ApplicationPaths.Temp, "PluginInstall_" + Guid.NewGuid());
        ZipFile.ExtractToDirectory(_pluginInstallFilePath, _tempExtractPath);

        // 读取并解析 manifest 文件
        string manifestPath = Path.Combine(_tempExtractPath, MANIFEST_FILE_NAME);
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("插件缺少 manifest 文件。");

        string manifestJson = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);

        if (manifest is null)
        {
            _logger.Error("插件 manifest 文件格式错误。");
            return;
        }

        PluginManifest = manifest;
        Directory.Delete(_tempExtractPath, true);
    }

    public DelegateCommand BeginInstallCommand { get; }

    /// <summary>
    /// 开始安装
    /// </summary>
    private void BeginInstall()
    {
        if (PluginManifest is null)
        {
            _logger.Error("请先加载插件。");
            return;
        }
        _regionManager.RequestNavigate(
            RegionNames.MainRegion,
            ViewNames.Installing,
            new NavigationParameters
            {
                { "FilePath", _pluginInstallFilePath },
            });
    }

    public DelegateCommand ReselectPluginInstallFileCommand { get; }
    private void ReselectPluginInstallFile()
    {
        _regionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.PickPluginInstallFile);
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (navigationContext.Parameters["PluginInstallFilePath"] is not string filePath)
            return;

        _pluginInstallFilePath = filePath;
        LoadPlugin();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
        => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="regionManager"></param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public InstallPreparationViewModel(IRegionManager regionManager, IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        BeginInstallCommand = new DelegateCommand(BeginInstall);
        ReselectPluginInstallFileCommand = new DelegateCommand(ReselectPluginInstallFile);
    }
}
