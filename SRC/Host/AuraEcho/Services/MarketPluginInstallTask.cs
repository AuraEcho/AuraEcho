using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Enums;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Events;
using Prism.Mvvm;

namespace AuraEcho.Services;

public class MarketPluginInstallTask : BindableBase, ITransferTask
{
    private readonly IRemotePluginRepository _remotePluginRepository;
    private readonly IPluginInstallService _pluginInstallService;
    private readonly IPluginManager _pluginManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IClientSession _clientSession;
    private readonly ILocalPluginRepository _localPluginRepository;
    private readonly MarketPlugin _plugin;
    protected CancellationTokenSource _cts;
    private bool _inProgress;

    public string Id => _plugin.PluginInfo.Id.ToString();
    public string Name => _plugin.PluginInfo.DisplayName;
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
        get => field;
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

            Status = MarketPluginInstallStatus.Downloading;
            string packageFilePath = await DownloadAsync(_cts.Token);

            Status = MarketPluginInstallStatus.Installing;
            await InstallAsync(packageFilePath, _cts.Token);

            Status = MarketPluginInstallStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            Status = MarketPluginInstallStatus.Canceled;
        }
        catch (Exception ex)
        {
            Status = MarketPluginInstallStatus.Failed;
        }
        finally
        {
            _inProgress = false;
        }
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
        IRemotePluginRepository remotePluginRepository,
        IPluginInstallService pluginInstallService,
        IPluginManager pluginManager,
        IEventAggregator eventAggregator,
        ILocalPluginRepository localPluginRepository,
        IClientSession clientSession,
        MarketPlugin plugin)
    {
        _plugin = plugin;
        _clientSession = clientSession;
        _localPluginRepository = localPluginRepository;
        _remotePluginRepository = remotePluginRepository;
        _pluginInstallService = pluginInstallService;
        _pluginManager = pluginManager;
        _eventAggregator = eventAggregator;
    }

    // 获取
    private async Task AcquireAsync()
    {
        Task<bool> acquireTask = _remotePluginRepository.AcquireAsync(_clientSession.CurrentUser.Id, _plugin.PluginInfo.Id);
        await Task.WhenAll(Task.Delay(TimeSpan.FromSeconds(1)), acquireTask);
        bool isSuccess = await acquireTask;

        if (!isSuccess) throw new Exception("Plugin acquire failed");
    }

    // 下载
    private async Task<string> DownloadAsync(CancellationToken token)
    {
        var pluginInstallerFilePath = Path.Combine(ApplicationPaths.Temp, $"{_plugin.PluginInfo.Id}.plix");
        Progress<double> progressHandler = new Progress<double>(p => Progress = p);
        await Task.Delay(TimeSpan.FromSeconds(0.5));

        Task<bool> downloadTask =
            _remotePluginRepository.DownloadLatestAsync(
                _plugin.PluginInfo.Id,
                "stable",
                pluginInstallerFilePath,
                progressHandler);
        await Task.WhenAll(downloadTask, Task.Delay(TimeSpan.FromSeconds(0.5), token));
        var result = await downloadTask;

        if (!result) throw new Exception("Plugin download failed");

        return pluginInstallerFilePath;
    }
    // 安装
    private async Task InstallAsync(string packageFilePath, CancellationToken token)
    {
        Task<LocalPluginModel> installlTask = _pluginInstallService.InstallAsync(packageFilePath);
        await Task.WhenAll(installlTask, Task.Delay(TimeSpan.FromSeconds(0.5), token));
        var localPlugin = await installlTask;
        File.Delete(packageFilePath);
        if (localPlugin is null) throw new Exception("Plugin install failed");
        var addUserPlugin = await _localPluginRepository.AddUserPluginAsync(_clientSession.CurrentUser.Id, localPlugin.Id);

        var loadPluginTask = _pluginManager.LoadPluginAsync(addUserPlugin);
        await Task.WhenAll(loadPluginTask, Task.Delay(TimeSpan.FromSeconds(0.5), token));
        var installResult = await loadPluginTask;
        if (!installResult) throw new Exception("Plugin load failed");

        _eventAggregator.GetEvent<PluginInstalledEvent>().Publish(addUserPlugin);
    }
}
