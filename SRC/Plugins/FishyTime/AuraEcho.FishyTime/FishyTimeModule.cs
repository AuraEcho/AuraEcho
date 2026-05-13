using AuraEcho.FishyTime.Themes;
using AuraEcho.FishyTime.Views;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Ioc;
using System.Threading.Tasks;
using System.Windows;

namespace AuraEcho.FishyTime;

public class FishyTimeModule : IPlugin
{
    private readonly ResourceDictionary _lightTheme = new FishyTimeLightTheme();
    private readonly ResourceDictionary _darkTheme = new FishyTimeDarkTheme();

    public string EntryViewName => nameof(FishyTimeHome);

    public AppSettingsItem GetSettings()
    {
        return new AppSettingsItem
        {
            Name = "FISHYTIME",
            ViewName = nameof(FishyTimeSettings)
        };
    }

    public ResourceDictionary GetThemeResource(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Light => _lightTheme,
            AppTheme.Dark => _darkTheme,
            _ => null
        };
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<FishyTimeHome>();
        containerRegistry.RegisterForNavigation<FishyTimeSettings>();
    }

    public Task SetupAsync(IContainerProvider containerProvider)
    {
        return Task.CompletedTask;
    }
}