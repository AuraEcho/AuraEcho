using System;
using System.Collections.Generic;
using System.Windows;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Design;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Prism.Mvvm;
using AuraEcho.Design.Themes;
namespace AuraEcho.Services;

public class ThemeManager : BindableBase, IThemeManager
{
    private readonly ILogger<ThemeManager> _logger;
    private readonly IPluginManager _pluginManager;
    private AppTheme _currentTheme;
    private readonly List<ResourceDictionary> _themeResources = [];
    private bool _isInitialized;

    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            bool isUpdated = SetProperty(ref _currentTheme, value);

            if (isUpdated || !_isInitialized)
            {
                ApplyTheme(value);
            }
            _isInitialized = true;
        }
    }

    public ThemeManager(ILogger<ThemeManager> logger, IPluginManager pluginManager)
    {
        _logger = logger;
        _pluginManager = pluginManager;

        SystemEvents.UserPreferenceChanged += UserPreferenceChanged;
    }

    public void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General) return;

        if (CurrentTheme != AppTheme.FollowSystem) return;

        var systemTheme = GetSystemTheme();
        ApplyTheme(systemTheme);
    }

    public void ApplyTheme(AppTheme appTheme)
    {
        AppTheme realTheme = appTheme == AppTheme.FollowSystem ? GetSystemTheme() : appTheme;
        try
        {
            ClearTheme();

            ResourceDictionary hostThemeResources = GetHostThemeResource(realTheme);
            List<ResourceDictionary> pluginThemeResources = GetPluginThemeResources(realTheme);

            _themeResources.Add(hostThemeResources);
            _themeResources.AddRange(pluginThemeResources);

            Application.Current.Resources.MergedDictionaries.Add(hostThemeResources);
            pluginThemeResources.ForEach(Application.Current.Resources.MergedDictionaries.Add);

            _logger.LogDebug("主题切换成功：{Theme} (Host + {PluginResourceCount} 插件资源)", realTheme, pluginThemeResources.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换主题失败：{Theme}", realTheme);
        }
    }

    private List<ResourceDictionary> GetPluginThemeResources(AppTheme appTheme)
    {
        var resources = new List<ResourceDictionary>();
        foreach (var plugin in _pluginManager.Plugins)
        {
            var pluginResource = plugin.GetThemeResource(appTheme);
            var fellBack = false;

            // 如果插件不支持当前具体主题，尝试基础主题（Light/Dark）
            if (pluginResource is null)
            {
                var baseTheme = appTheme.GetBaseTheme();
                if (baseTheme != appTheme)
                {
                    pluginResource = plugin.GetThemeResource(baseTheme);
                    fellBack = pluginResource is not null;
                }
            }

            if (pluginResource is not null)
            {
                resources.Add(pluginResource);
                _logger.LogDebug("插件主题资源: {PluginName} -> {Theme}{FallbackNote}",
                    plugin.PluginName, appTheme, fellBack ? $" (回退到 {appTheme.GetBaseTheme()})" : "");
            }
        }
        return resources;
    }

    private static ResourceDictionary GetHostThemeResource(AppTheme appTheme)
    {
        return appTheme switch
        {
            AppTheme.Light => LightTheme.Instance,
            AppTheme.Dark => DarkTheme.Instance,
            _ => throw new ArgumentOutOfRangeException(nameof(appTheme), appTheme, null)
        };
    }

    public void ClearTheme()
    {
        foreach (var dict in _themeResources)
            Application.Current.Resources.MergedDictionaries.Remove(dict);

        _themeResources.Clear();
    }

    public static AppTheme GetSystemTheme()
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        if (key is null) return AppTheme.Light;

        int appsUseLightTheme = (int)key.GetValue("AppsUseLightTheme", -1);
        key.Close();

        // 0: 暗色 1：亮色
        return appsUseLightTheme == 0 ? AppTheme.Dark : AppTheme.Light;
    }

    public void AttachPluginTheme(AppPlugin plugin)
    {
        AppTheme realTheme = CurrentTheme == AppTheme.FollowSystem ? GetSystemTheme() : CurrentTheme;
        var pluginThemeResource = plugin.GetThemeResource(realTheme);

        // 如果插件不支持当前具体主题，尝试基础主题（Light/Dark）
        if (pluginThemeResource is null)
        {
            var baseTheme = realTheme.GetBaseTheme();
            if (baseTheme != realTheme)
                pluginThemeResource = plugin.GetThemeResource(baseTheme);
        }

        if (pluginThemeResource is null) return;

        Application.Current.Resources.MergedDictionaries.Add(pluginThemeResource);
        _themeResources.Add(pluginThemeResource);
    }

    public void AttachPluginThemes(IEnumerable<AppPlugin> plugins)
    {
        plugins.ForEach(AttachPluginTheme);
    }
}
