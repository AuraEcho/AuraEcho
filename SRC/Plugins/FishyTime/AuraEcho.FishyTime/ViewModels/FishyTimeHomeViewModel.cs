using AuraEcho.FishyTime.Models;
using AuraEcho.FishyTime.Utils;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AuraEcho.FishyTime.ViewModels;

public class FishyTimeHomeViewModel : BindableBase, INavigationAware
{
    #region private members
    private readonly IPathProvider _pathProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILicenseService _licenseService;
    private readonly IPurchaseCoordinator _purchaseCoordinator;
    private FishyTimeConfig _fishyTimeConfig;
    private const string FishyTimeConfigFileName = "FishyTimeConfig.json";
    private ObservableCollection<Win32Window> _win32Windows = [];
    private ObservableCollection<WindowMaskMode> _windowMaskModes =
    [
        WindowMaskMode.MouseLeave,
        WindowMaskMode.HotZone,
        WindowMaskMode.Spotlight
    ];
    private Guid _pluginId;
    #endregion

    public ResourceLicense LicenseInfo
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ObservableCollection<Win32Window> Win32Windows
    {
        get => _win32Windows;
        set => SetProperty(ref _win32Windows, value);
    }

    public ObservableCollection<WindowMaskMode> WindowMaskModes
    {
        get => _windowMaskModes;
        set => SetProperty(ref _windowMaskModes, value);
    }

    public FishyTimeConfig FishyTimeConfig
    {
        get => _fishyTimeConfig;
        set => SetProperty(ref _fishyTimeConfig, value);
    }

    public DelegateCommand<IntPtr?> AddWin32WindowCommand { get; }
    private async void AddWin32Window(IntPtr? handle)
    {
        if (handle is null or 0) return;

        var isExist = Win32Windows.Any(w => w.Handle == handle || w.MaskHandle == handle);
        if (isExist) return;

        var win32Window = await Win32Window.AttachAsync(handle.Value);
        win32Window.Closed += Win32WindowClosed;

        Win32Windows.Add(win32Window);
    }

    private void Win32WindowClosed(Win32Window win32Window)
    {
        if (win32Window is null) return;

        RemoveWin32Window(win32Window);
    }

    public DelegateCommand NavigationToPurchaseCommand { get; }
    private async void NavigationToPurchase()
    {
        var result = await _purchaseCoordinator.RequestPurchaseAsync(_pluginId);
        if (result)
        {
            var licenseInfo = await _licenseService.GetResourceLicenseAsync(_pluginId);
            LicenseInfo = licenseInfo.IsValid ? licenseInfo : null;
        }
    }

    public DelegateCommand<Win32Window> RemoveWin32WindowCommand { get; }
    private void RemoveWin32Window(Win32Window win32Window)
    {
        if (win32Window is null) return;

        if (Win32Windows.Remove(win32Window))
        {
            win32Window.Deattach();
        }
    }

    public DelegateCommand LoadDataCommand { get; }
    private void LoadData()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FishyTimeConfigFilePath));

        if (!File.Exists(FishyTimeConfigFilePath))
        {
            FishyTimeConfig = new FishyTimeConfig
            {
                IsWindowMaskEnabled = false
            };
            return;
        }

        string configJson = File.ReadAllText(FishyTimeConfigFilePath);
        var fishyTimeConfig = JsonSerializer.Deserialize<FishyTimeConfig>(configJson);
        if (fishyTimeConfig is null)
        {
            FishyTimeConfig = new FishyTimeConfig
            {
                IsWindowMaskEnabled = false
            };
            return;
        }

        FishyTimeConfig = fishyTimeConfig;
    }

    private string FishyTimeConfigFilePath
        => Path.Combine(_pathProvider.DataRootPath, "FishyTime", FishyTimeConfigFileName);

    public DelegateCommand SaveDataCommand { get; }
    private void SaveData()
    {
        if (FishyTimeConfig is null) return;

        var configJson = JsonSerializer.Serialize(FishyTimeConfig);
        File.WriteAllText(FishyTimeConfigFilePath, configJson);
    }

    public FishyTimeHomeViewModel(IPathProvider pathProvider, IEventAggregator eventAggregator, ILicenseService licenseService, IPurchaseCoordinator purchaseCoordinator)
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _purchaseCoordinator = purchaseCoordinator ?? throw new ArgumentNullException(nameof(purchaseCoordinator));
        _eventAggregator.GetEvent<AppLanguageChangedEvent>().Subscribe(AppLanguageChanged);

        Win32Windows = [];
        AddWin32WindowCommand = new DelegateCommand<nint?>(AddWin32Window);
        RemoveWin32WindowCommand = new DelegateCommand<Win32Window>(RemoveWin32Window);

        LoadDataCommand = new DelegateCommand(LoadData);
        SaveDataCommand = new DelegateCommand(SaveData);
        NavigationToPurchaseCommand = new DelegateCommand(NavigationToPurchase);
    }

    private void AppLanguageChanged(AppLanguage language)
    {
        var targetCultureInfo = language switch
        {
            AppLanguage.ChineseSimplified => new CultureInfo("zh-CN"),
            AppLanguage.English => new CultureInfo("en-US"),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };

        FishyTimeResources.ChangeCulture(targetCultureInfo);
    }

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        _pluginId = navigationContext.Parameters.GetValue<Guid>("PluginId");

        var licenseInfo = await _licenseService.GetResourceLicenseAsync(_pluginId);
        if (licenseInfo.IsValid)
        {
            LicenseInfo = licenseInfo;
        }
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
