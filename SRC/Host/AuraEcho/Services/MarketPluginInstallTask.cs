using AuraEcho.Telemetry;
using AuraEcho.Cloud.V1;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Domain;
using AuraEcho.Persistence.Contracts;
using AuraEcho.Enums;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Strings;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuraEcho.Services;

public class MarketPluginInstallTask : BindableBase, ITransferTask
{
    private readonly ApiClient _apiClient;
    private readonly IPluginInstallService _pluginInstallService;
    private readonly IPluginManager _pluginManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IClientSession _clientSession;
    private readonly ILocalPluginRepository _localPluginRepository;
    private readonly IAuraToastService _auraToastService;
    private readonly ITelemetryService _telemetry;
    private readonly MarketPlugin _plugin;
    protected CancellationTokenSource _cts;
    private bool _inProgress;

    public string Id => _plugin.PluginInfo.Id.ToString();
    public string Name => _plugin.PluginInfo.Name;
    public TransferType Type => TransferType.Download;

    public double Progress
    {
        get => field;
        protected set => SetProperty(ref field, value);
    }

    public long TotalSize
    {
        get => field;
        protected set => SetProperty(ref field, value);
    }

    public long TransferredSize
    {
        get => field;
        protected set
        {
            if (SetProperty(ref field, value))
            {
                if (TotalSize > 0)
                    Progress = Math.Round((double)field / TotalSize * 100, 2);
            }
        }
    }

    public MarketPluginInstallStatus Status
    {
        get;
        protected set => SetProperty(ref field, value);
    }

    public async Task Start()
    {
        if (_inProgress) return;
        _cts = new CancellationTokenSource();
        try
        {
            _inProgress = true;
            if (_plugin.Status == MarketPluginStatus.None)
            {
                Status = MarketPluginInstallStatus.Acquiring;
                await AcquireAsync();
            }
            _plugin.Status = MarketPluginStatus.Acquired;

            Status = MarketPluginInstallStatus.Downloading;
            string packageFilePath = await DownloadAsync(_cts.Token);

            Status = MarketPluginInstallStatus.Installing;
            var localPlugin = await InstallAsync(packageFilePath, _cts.Token);
            var userPlugin = await RegisterUserPluginAsync(localPlugin);
            var loadedPlugin = await LoadPluginAsync(userPlugin, _cts.Token);

            _plugin.Status = MarketPluginStatus.Installed;
            Status = MarketPluginInstallStatus.Completed;
            _telemetry?.TrackEvent("Plugin.InstallCompleted", new System.Collections.Generic.Dictionary<string, string>
            {
                ["pluginId"] = _plugin.PluginInfo.Id.ToString()
            });
            _eventAggregator.GetEvent<PluginInstalledEvent>().Publish(loadedPlugin);
        }
        catch (OperationCanceledException)
        {
            Status = MarketPluginInstallStatus.Canceled;
            _telemetry?.TrackEvent("Plugin.InstallCanceled", new System.Collections.Generic.Dictionary<string, string>
            {
                ["pluginId"] = _plugin.PluginInfo.Id.ToString(),
                ["stage"] = Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _telemetry?.TrackEvent("Plugin.InstallFailed", new System.Collections.Generic.Dictionary<string, string>
            {
                ["pluginId"] = _plugin.PluginInfo.Id.ToString(),
                ["stage"] = Status.ToString(),
                ["exceptionType"] = ex.GetType().Name,
                ["reason"] = ex.Message
            });
            _auraToastService.Show(String.Format(Labels.MarketplacePluginDetails_InstallError, _plugin.PluginInfo.Name), ToastLevel.Error);
            Status = MarketPluginInstallStatus.Failed;
        }
        finally
        {
            Progress = 0;
            _inProgress = false;
        }
    }

    private async Task<(bool, InstalledPluginModel?)> IsInstalled(MarketPlugin mp)
    {
        var latestResponse = await _apiClient.Plugin.GetLatestAsync(mp.PluginInfo.Id);
        var latestVersionPackInfo = latestResponse?.ToPluginPackage();
        Version? marketPluginLatestVersion =
            latestVersionPackInfo is null
            ? null
            : Version.Parse(latestVersionPackInfo.Version);

        var localPlugins = await _localPluginRepository.GetLocalPluginsAsync();
        InstalledPluginModel? installedPlugin = 
            localPlugins.FirstOrDefault(lp => 
                lp.Id == mp.PluginInfo.Id && 
                Version.Parse(lp.Version!) == marketPluginLatestVersion);

        return (installedPlugin is not null, installedPlugin);
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public static MarketPluginInstallTask CreateAsCompleted(MarketPlugin plugin)
     => new(plugin)
     {
         Status = MarketPluginInstallStatus.Completed,
         Progress = 100
     };

    private MarketPluginInstallTask(MarketPlugin plugin)
    {
        _plugin = plugin;
    }

    public MarketPluginInstallTask(
        ApiClient apiClient,
        IPluginInstallService pluginInstallService,
        IPluginManager pluginManager,
        IEventAggregator eventAggregator,
        ILocalPluginRepository localPluginRepository,
        IClientSession clientSession,
        IAuraToastService auraToastService,
        ITelemetryService telemetry,
        MarketPlugin plugin)
    {
        _plugin = plugin;
        _clientSession = clientSession;
        _auraToastService = auraToastService;
        _localPluginRepository = localPluginRepository;
        _apiClient = apiClient;
        _pluginInstallService = pluginInstallService;
        _pluginManager = pluginManager;
        _eventAggregator = eventAggregator;
        _telemetry = telemetry;
    }

    // 获取
    private async Task AcquireAsync()
    {
        var acquireTask = _apiClient.Plugin.AcquireAsync(_plugin.PluginInfo.Id);
        await Task.WhenAll(Task.Delay(TimeSpan.FromSeconds(1)), acquireTask);
        var result = await acquireTask;

        if (result is null) throw new Exception("Plugin acquire failed");
    }

    // 下载
    private async Task<string> DownloadAsync(CancellationToken token)
    {
        if (await IsInstalled(_plugin) is (true, _))
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5), token);
            Progress = 50;
            await Task.Delay(TimeSpan.FromSeconds(0.5), token);
            Progress = 100;
            return String.Empty;
        }

        var pluginInstallerFilePath = Path.Combine(ApplicationPaths.Temp, $"{_plugin.PluginInfo.Id}.{FileExtensionNames.PluginFile}");
        Progress<double> progressHandler = new Progress<double>(p => Progress = p);
        await Task.Delay(TimeSpan.FromSeconds(0.5));

        var latestResponse = await _apiClient.Plugin.GetLatestAsync(_plugin.PluginInfo.Id);
        PluginPackage latestVersionPackInfo = latestResponse?.ToPluginPackage();

        Task<bool> downloadTask =
            _apiClient.File.DownloadFileAsync(
                latestVersionPackInfo.FileUrl,
                pluginInstallerFilePath,
                progressHandler);
        await Task.WhenAll(downloadTask, Task.Delay(TimeSpan.FromSeconds(0.5), token));
        var result = await downloadTask;

        if (!result) throw new Exception("Plugin download failed");

        return pluginInstallerFilePath;
    }
    /// <summary>
    /// 安装
    /// </summary>
    /// <param name="packageFilePath"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<InstalledPluginModel> InstallAsync(string packageFilePath, CancellationToken token)
    {
        if (await IsInstalled(_plugin) is (true, var installedPlugin))
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5), token);
            return installedPlugin!;
        }
        Task<InstalledPluginModel> installlTask = _pluginInstallService.InstallAsync(packageFilePath);
        await Task.WhenAll(installlTask, Task.Delay(TimeSpan.FromSeconds(0.5), token));
        var localPlugin = await installlTask;
        File.Delete(packageFilePath);

        return localPlugin ?? throw new Exception("Plugin install failed");
    }

    /// <summary>
    /// 注册用户插件关系
    /// </summary>
    /// <returns></returns>
    private async Task<UserPluginModel> RegisterUserPluginAsync(InstalledPluginModel plugin)
    {
        var addUserPlugin = await _localPluginRepository.AddUserPluginAsync(_clientSession.CurrentUser.Id, plugin.Id);
        return addUserPlugin;
    }

    /// <summary>
    /// 加载插件
    /// </summary>
    /// <returns></returns>
    private async Task<AppPlugin> LoadPluginAsync(UserPluginModel up, CancellationToken token)
    {
        var loadPluginTask = _pluginManager.LoadPluginAsync(up);
        await Task.WhenAll(loadPluginTask, Task.Delay(TimeSpan.FromSeconds(0.5), token));
        var installResult = await loadPluginTask;

        return installResult ?? throw new Exception("Plugin load failed");
    }
}
