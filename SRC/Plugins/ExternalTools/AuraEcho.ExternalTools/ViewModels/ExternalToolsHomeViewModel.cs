using AuraEcho.ExternalTools.Constants;
using AuraEcho.ExternalTools.Contracts;
using AuraEcho.ExternalTools.Events;
using AuraEcho.ExternalTools.Models;
using AuraEcho.ExternalTools.Utils;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace AuraEcho.ExternalTools.ViewModels;

public class ExternalToolsHomeViewModel : BindableBase
{
    #region private members
    private readonly IExternalToolsRepository _repository;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;
    private ObservableCollection<ExternalTool> _externalTools;
    #endregion

    public ObservableCollection<ExternalTool> ExternalTools
    {
        get => _externalTools;
        set => SetProperty(ref _externalTools, value);
    }

    public DelegateCommand LoadDataCommand { get; }
    private void LoadData()
    {
        ExternalTools = [.. _repository.GetExternalTools() ];
    }

    public DelegateCommand<ExternalTool> LunchExternalToolCommand { get; }
    private void LunchExternalTool(ExternalTool externalTool)
    {
        if (externalTool is null) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = externalTool.Command,
            UseShellExecute = true,
            Arguments = externalTool.Arguments
        });
    }

    public DelegateCommand AddExternalToolCommand { get; }
    private void AddExternalTool()
    {
        _regionManager.RequestNavigate(HostRegionNames.DialogRegion, ExternalToolsViewNames.AddExternalTool);
    }

    public DelegateCommand<ExternalTool> RemoveExternalToolCommand { get; }
    private void RemoveExternalTool(ExternalTool tool)
    {
        if (tool is null) return;
        if (!ExternalTools.Contains(tool)) return;

        ExternalTools.Remove(tool);
        _repository.DeleteExternalTool(tool.Id);
    }

    public DelegateCommand<ExternalTool> EditExternalToolCommand { get; }
    private void EditExternalTool(ExternalTool tool)
    {
        _regionManager.RequestNavigate(
            HostRegionNames.DialogRegion,
            ExternalToolsViewNames.EditExternalTool,
            new NavigationParameters
            {
                { "ExternalTool", tool }
            });
    }

    public ExternalToolsHomeViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, IExternalToolsRepository repository)
    {
        _regionManager = regionManager;
        _repository = repository;
        _eventAggregator = eventAggregator;
        _eventAggregator.GetEvent<ExternalToolAddedEvent>().Subscribe(ExternalToolAdded);
        _eventAggregator.GetEvent<ExternalToolUpdatedEvent>().Subscribe(ExternalToolUpdated);
        _eventAggregator.GetEvent<AppLanguageChangedEvent>().Subscribe(AppLanguageChanged);

        LoadDataCommand = new DelegateCommand(LoadData);
        AddExternalToolCommand = new DelegateCommand(AddExternalTool);
        RemoveExternalToolCommand = new DelegateCommand<ExternalTool>(RemoveExternalTool);
        EditExternalToolCommand = new DelegateCommand<ExternalTool>(EditExternalTool);
        LunchExternalToolCommand = new DelegateCommand<ExternalTool>(LunchExternalTool);
    }

    private void AppLanguageChanged(AppLanguage language)
    {
        var targetCultureInfo = language switch
        {
            AppLanguage.ChineseSimplified => new CultureInfo("zh-CN"),
            AppLanguage.English => new CultureInfo("en-US"),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };

        ExternalToolsResources.ChangeCulture(targetCultureInfo);
    }

    private void ExternalToolUpdated(ExternalTool tool)
    {
        if (tool is null) throw new Exception("Tool cannot be null");

        var existingTool = ExternalTools.FirstOrDefault(t => t.Id == tool.Id) ?? throw new Exception("Tool not found");

        existingTool.Name = tool.Name;
        existingTool.Command = tool.Command;
        existingTool.Arguments = tool.Arguments;
        existingTool.Type = tool.Type;
        _repository.UpdateExternalTool(existingTool);
    }

    private void ExternalToolAdded(ExternalTool tool)
    {
        if (tool is null) return;

        ExternalTools.Add(tool);
        _repository.AddExternalTool(tool);
    }
}
