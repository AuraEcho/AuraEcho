using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Events;

namespace AuraEcho.Services;

public class PluginDownloadTask : BaseTransferTask
{
    private readonly IRemotePluginRepository _remotePluginRepository;
    private readonly IPluginInstallService _pluginInstallService;
    private readonly IPluginManager _pluginManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly Guid _pluginId;

    public static PluginDownloadTask CreateAsCompleted()
     => new()
     {
         Status = TransferStatus.Completed,
         Progress = 100
     };

    private PluginDownloadTask() : base(string.Empty, string.Empty, TransferType.Download)
    {
    }

    public PluginDownloadTask(
        IRemotePluginRepository remotePluginRepository,
        IPluginInstallService pluginInstallService,
        IPluginManager pluginManager,
        IEventAggregator eventAggregator,
        Guid pluginId,
        string taskName)
        : base(pluginId.ToString(), taskName, TransferType.Download)
    {
        _pluginId = pluginId;
        _remotePluginRepository = remotePluginRepository;
        _pluginInstallService = pluginInstallService;
        _pluginManager = pluginManager;
        _eventAggregator = eventAggregator;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var pluginInstallerFilePath = Path.Combine(ApplicationPaths.Temp, $"{_pluginId}.plix");
        Progress<double> progressHandler = new Progress<double>(p => Progress = p);
        await Task.Delay(TimeSpan.FromSeconds(0.2), token);

        Task<bool> downloadTask = 
            _remotePluginRepository.DownloadLatestAsync(
                _pluginId,
                "stable",
                pluginInstallerFilePath,
                progressHandler);
        await Task.WhenAll(downloadTask, Task.Delay(TimeSpan.FromSeconds(0.5), token));
        var result = await downloadTask;
        if (!result) throw new Exception("Plugin download failed");

        Status = TransferStatus.Processing;

        Task<PluginRegistryModel> installlTask = _pluginInstallService.InstallAsync(pluginInstallerFilePath);
        await Task.WhenAll(installlTask, Task.Delay(TimeSpan.FromSeconds(0.5), token));
        var pluginRegistry = await installlTask;
        File.Delete(pluginInstallerFilePath);
        if (pluginRegistry is null) throw new Exception("Plugin install failed");

        var loadPluginTask = _pluginManager.LoadPluginAsync(pluginRegistry);
        await Task.WhenAll(loadPluginTask, Task.Delay(TimeSpan.FromSeconds(0.5), token));
        var installResult = await loadPluginTask;
        if (!installResult) throw new Exception("Plugin load failed");

        _eventAggregator.GetEvent<PluginInstalledEvent>().Publish(pluginRegistry);
    }
}
