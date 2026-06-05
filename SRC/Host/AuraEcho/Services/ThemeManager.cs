using System;
using System.Collections.Generic;
using System.Windows;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using AuraEcho.Design;
using Microsoft.Win32;
using Prism.Mvvm;
using AuraEcho.Design.Themes;
namespace AuraEcho.Services;

public class ThemeManager : BindableBase, IThemeManager
{
    private readonly IAppLogger _logger;
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

    public ThemeManager(IAppLogger logger, IPluginManager pluginManager)
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

            _logger.Debug($"主题切换成功：{realTheme} (Host + {pluginThemeResources.Count} 插件资源)");
        }
        catch (Exception ex)
        {
            _logger.Error($"切换主题失败：{realTheme}，异常：{ex.Message}");
        }
    }

    private List<ResourceDictionary> GetPluginThemeResources(AppTheme appTheme)
    {
        var resources = new List<ResourceDictionary>();
        foreach (var plugin in _pluginManager.Plugins)
        {
            var pluginResource = plugin.GetThemeResource(appTheme);
            if (pluginResource != null)
                resources.Add(pluginResource);
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
        if (pluginThemeResource is null) return;

        Application.Current.Resources.MergedDictionaries.Add(pluginThemeResource);
        _themeResources.Add(pluginThemeResource);
    }

    public void AttachPluginThemes(IEnumerable<AppPlugin> plugins)
    {
        plugins.ForEach(AttachPluginTheme);
    }
}
