using System;
using System.Globalization;
using System.Windows.Interop;
using System.Windows.Media;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;
using AuraEcho.Interfaces;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Models;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace AuraEcho.ViewModels;

public class GeneralSettingsViewModel : BindableBase
{
    #region private members
    private readonly IEventAggregator _eventAggregator;
    private readonly IThemeManager _themeManager;
    private readonly IHostSettingsProvider _hostSettingsProvider;
    private AppLanguage _appLanguage;
    private AppTheme _appTheme;
    private bool _runAtBoot;
    private bool _hardwareAcceleration;
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
            }
        }
    }

    private void LanguageChanged(AppLanguage language)
    {
        var targetCultureInfo = language switch
        {
            AppLanguage.ChineseSimplified => new CultureInfo("zh-CN"),
            AppLanguage.English => new CultureInfo("en-US"),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };

        ApplicationResources.ChangeCulture(targetCultureInfo);

        _eventAggregator.GetEvent<AppLanguageChangedEvent>().Publish(language);
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
            SetProperty(ref _runAtBoot, value);
            SetRunAtBoot(value);
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
        var settings = _hostSettingsProvider.LoadHostSettings();
        AppLanguage = settings.AppLanguage;
        AppTheme = settings.AppTheme;
        HardwareAcceleration = settings.HardwareAcceleration;
        RunAtBoot = CheckRunAtBoot();
    }
    private void SaveSettings()
    {
        var settings = new HostSettings
        {
            AppLanguage = AppLanguage,
            AppTheme = AppTheme,
            HardwareAcceleration = HardwareAcceleration
        };
        _hostSettingsProvider.SaveHostSettings(settings);
    }

    public GeneralSettingsViewModel(IEventAggregator eventAggregator, IThemeManager themeManager, IHostSettingsProvider hostSettingsProvider)
    {
        _hostSettingsProvider = hostSettingsProvider;
        _eventAggregator = eventAggregator;
        _themeManager = themeManager;

        LoadSettingsCommand = new DelegateCommand(LoadSettings);
    }
}
