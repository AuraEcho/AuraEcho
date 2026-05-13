using Microsoft.EntityFrameworkCore;
using AuraEcho.ExternalTools.Contracts;
using AuraEcho.ExternalTools.Data;
using AuraEcho.ExternalTools.Repositories;
using AuraEcho.ExternalTools.Themes;
using AuraEcho.ExternalTools.Views;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Ioc;
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;

namespace AuraEcho.ExternalTools;

public class ExternalToolsModule : IPlugin
{
    private readonly ResourceDictionary _lightTheme = new ExternalToolsLightTheme();
    private readonly ResourceDictionary _darkTheme = new ExternalToolsDarkTheme();

    public string EntryViewName => nameof(ExternalToolsHome);

    public AppSettingsItem GetSettings()
    {
        return new()
        {
            Name = "EXTERNAL TOOLS",
            ViewName = nameof(ExternalToolsSettings),
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
        containerRegistry.RegisterForNavigation<ExternalToolsHome>();
        containerRegistry.RegisterForNavigation<AddExternalTool>();
        containerRegistry.RegisterForNavigation<EditExternalTool>();
        containerRegistry.RegisterForNavigation<ExternalToolsSettings>();

        containerRegistry.Register<IExternalToolsRepository, ExternalToolsRepository>();
    }

    public async Task SetupAsync(IContainerProvider containerProvider)
    {
        IPathProvider pathProvider = containerProvider.Resolve<IPathProvider>();
        Directory.CreateDirectory(Path.Combine(pathProvider.DataRootPath, "ExternalTools"));

        using var dbContext = containerProvider.Resolve<ExternalToolsDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await dbContext.Database.MigrateAsync();
        }
    }
}