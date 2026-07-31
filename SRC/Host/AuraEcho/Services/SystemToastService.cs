using System;
using System.IO;
using System.Windows;
using AuraEcho.Constants;
using AuraEcho.Core.Events;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.Strings;
using AuraEcho.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using Prism.Events;
using Prism.Ioc;

namespace AuraEcho.Services;

public class SystemToastService : ISystemToastService
{
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<SystemToastService> _logger;
    private readonly ITelemetryService _telemetry;

    public SystemToastService(
        IContainerProvider containerProvider,
        IEventAggregator eventAggregator,
        ILogger<SystemToastService> logger,
        ITelemetryService telemetry)
    {
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _logger = logger;
        _telemetry = telemetry;

        // 点击通知时激活程序(即使程序已关闭)
        ToastNotificationManagerCompat.OnActivated += OnToastActivated;
    }

    public bool IsAppInForeground
    {
        get
        {
            Window? mainWindow = Application.Current?.MainWindow;
            if (mainWindow is null) return false;

            return Application.Current.Dispatcher.Invoke(() =>
                mainWindow.IsVisible
                && mainWindow.IsActive
                && mainWindow.WindowState != WindowState.Minimized);
        }
    }

    public void NotifyPluginInstalled(AppPlugin plugin)
    {
        if (plugin is null) return;

        try
        {
            var builder =
                new ToastContentBuilder()
                    .AddArgument(SystemToastArguments.Action, SystemToastArguments.OpenPluginAction)
                    .AddArgument(SystemToastArguments.PluginId, plugin.PluginId.ToString())
                    .AddText(String.Format(Labels.SystemToast_PluginInstalledTitle, plugin.PluginName))
                    .AddText(Labels.SystemToast_PluginInstalledMessage);

            string? iconPath = GetPluginIconPath(plugin);
            if (iconPath is not null)
                builder.AddAppLogoOverride(new Uri(iconPath), ToastGenericAppLogoCrop.Default);

            builder.Show();

            _telemetry?.TrackEvent("SystemToast.PluginInstalledShown", new System.Collections.Generic.Dictionary<string, string>
            {
                ["pluginId"] = plugin.PluginId.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "推送插件安装完成的系统通知失败：{PluginId}", plugin.PluginId);
        }
    }

    private static string? GetPluginIconPath(AppPlugin plugin)
    {
        if (String.IsNullOrWhiteSpace(plugin.WorkingDirectory) || String.IsNullOrWhiteSpace(plugin.Icon))
            return null;

        string iconPath = Path.Combine(plugin.WorkingDirectory, plugin.Icon);
        return File.Exists(iconPath) ? iconPath : null;
    }

    private void OnToastActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
    {
        try
        {
            ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
            if (!args.TryGetValue(SystemToastArguments.Action, out string? action)) return;
            if (action != SystemToastArguments.OpenPluginAction) return;
            if (!args.TryGetValue(SystemToastArguments.PluginId, out string? pluginIdText)) return;
            if (!Guid.TryParse(pluginIdText, out Guid pluginId)) return;

            _telemetry?.TrackEvent("SystemToast.Activated", new System.Collections.Generic.Dictionary<string, string>
            {
                ["action"] = action,
                ["pluginId"] = pluginId.ToString()
            });

            Application.Current?.Dispatcher.Invoke(() =>
            {
                _eventAggregator.GetEvent<RequestShowAppEvent>().Publish();
                _containerProvider.Resolve<IPluginLaunchService>().Launch(pluginId);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "处理系统通知点击事件失败：{Argument}", toastArgs.Argument);
        }
    }
}
