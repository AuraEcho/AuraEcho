using AuraEcho.Telemetry;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Interop;
using System.Windows.Media;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Strings;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Models;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using AuraEcho.PluginContracts.Interfaces;

namespace AuraEcho.ViewModels;

public class GeneralSettingsViewModel : BindableBase
{
    #region private members
    private readonly IEventAggregator _eventAggregator;
    private readonly IThemeManager _themeManager;
    private readonly IHostSettingsProvider _hostSettingsProvider;
    private readonly ITelemetryService _telemetryService;
    private AppLanguage _appLanguage;
    private AppTheme _appTheme;
    private bool _runAtBoot;
    private bool _hardwareAcceleration;
    private bool _telemetryEnabled;
    private bool _isLoadingSettings;
    #endregion

    public AppLanguage AppLanguage
    {
        get => _appLanguage;
        set
        {
            if (SetProperty(ref _appLanguage, value))
            {
                LanguageChanged(value);
                SaveSettings();
                TrackSettingChanged(nameof(AppLanguage), value.ToString());
            }
        }
    }

    private void LanguageChanged(AppLanguage language)
    {
        var targetCultureInfo = language switch
        {
            AppLanguage.ChineseSimplified => new CultureInfo("zh-CN"),
            AppLanguage.English => new CultureInfo("en-US"),
            AppLanguage.Korean => new CultureInfo("ko-KR"),
            AppLanguage.Japanese => new CultureInfo("ja-JP"),
            AppLanguage.ChineseTraditional => new CultureInfo("zh-TW"),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };

        LocalizationManager.ChangeCulture(targetCultureInfo);

        try
        {
            _eventAggregator.GetEvent<AppLanguageChangedEvent>().Publish(language);
        } 
        catch (Exception ex)
        {
            Debug.WriteLine($"{nameof(LanguageChanged)}: {ex}");
        }
    }

    public AppTheme AppTheme
    {
        get => _appTheme;
        set
        {
            bool isUpadted = SetProperty(ref _appTheme, value);
            if (isUpadted)
            {
                ApplyTheme();
                SaveSettings();
                TrackSettingChanged(nameof(AppTheme), value.ToString());
            }
        }
    }

    private void ApplyTheme()
    {
        _themeManager.CurrentTheme = AppTheme;
    }

    public bool HardwareAcceleration
    {
        get => _hardwareAcceleration;
        set
        {
            if (SetProperty(ref _hardwareAcceleration, value))
            {
                HardwareAccelerationChanged(value);
                SaveSettings();
                TrackSettingChanged(nameof(HardwareAcceleration), value ? "true" : "false");
            }
        }
    }

    private static void HardwareAccelerationChanged(bool isEnabled)
    {
        RenderOptions.ProcessRenderMode = isEnabled ? RenderMode.Default : RenderMode.SoftwareOnly;
    }

    public bool RunAtBoot
    {
        get => _runAtBoot;
        set
        {
            if (SetProperty(ref _runAtBoot, value))
            {
                SetRunAtBoot(value);
                TrackSettingChanged(nameof(RunAtBoot), value ? "true" : "false");
            }
        }
    }

    public bool TelemetryEnabled
    {
        get => _telemetryEnabled;
        set
        {
            if (SetProperty(ref _telemetryEnabled, value))
            {
                // 关闭前记录本次切换（关闭后事件将不再产生）
                TrackSettingChanged(nameof(TelemetryEnabled), value ? "true" : "false");
                SaveSettings();
                _telemetryService.IsEnabled = value;
            }
        }
    }

    private static bool CheckRunAtBoot()
    {
        using RegistryKey itemKeyRoot =
            Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);

        if (itemKeyRoot.GetValue("AuraEcho") is null) return false;

        using RegistryKey approvedKeyRoot =
            Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run");

        if (approvedKeyRoot.GetValue("AuraEcho") is not byte[] key) return true;

        return key[0] % 2 == 0;
    }

    private static void SetRunAtBoot(bool isEnabled)
    {
        using RegistryKey startupApprovedKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", true);
        if (startupApprovedKey.GetValue("AuraEcho") is not null)
            startupApprovedKey.DeleteValue("AuraEcho");

        using RegistryKey itemKeyRoot = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (isEnabled)
        {
            itemKeyRoot.SetValue("AuraEcho", $@"""{ApplicationPaths.LauncherPath}"" -hide", RegistryValueKind.String);
            return;
        }

        if (itemKeyRoot.GetValue("AuraEcho") is null) return;

        itemKeyRoot.DeleteValue("AuraEcho");
    }

    public DelegateCommand LoadSettingsCommand { get; }
    private void LoadSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var settings = _hostSettingsProvider.LoadHostSettings();
            AppLanguage = settings.AppLanguage;
            AppTheme = settings.AppTheme;
            HardwareAcceleration = settings.HardwareAcceleration;
            RunAtBoot = CheckRunAtBoot();
            TelemetryEnabled = settings.TelemetryEnabled;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }
    private void SaveSettings()
    {
        var settings = new HostSettings
        {
            AppLanguage = AppLanguage,
            AppTheme = AppTheme,
            HardwareAcceleration = HardwareAcceleration,
            TelemetryEnabled = TelemetryEnabled
        };
        _hostSettingsProvider.SaveHostSettings(settings);
    }

    /// <summary>
    /// 上报设置项变更。加载设置阶段（<see cref="_isLoadingSettings"/>）不上报，只记用户主动变更。
    /// </summary>
    private void TrackSettingChanged(string key, string value)
    {
        if (_isLoadingSettings) return;
        _telemetryService.TrackEvent("Settings.Changed", new System.Collections.Generic.Dictionary<string, string>
        {
            ["key"] = key,
            ["value"] = value
        });
    }

    public GeneralSettingsViewModel(
        IEventAggregator eventAggregator, 
        IThemeManager themeManager, 
        IHostSettingsProvider hostSettingsProvider,
        ITelemetryService telemetryService)
    {
        _hostSettingsProvider = hostSettingsProvider;
        _eventAggregator = eventAggregator;
        _themeManager = themeManager;
        _telemetryService = telemetryService;

        LoadSettingsCommand = new DelegateCommand(LoadSettings);
    }
}
