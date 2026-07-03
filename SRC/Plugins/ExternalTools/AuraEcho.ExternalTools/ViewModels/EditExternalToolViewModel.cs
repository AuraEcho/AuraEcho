using AuraEcho.ExternalTools.Events;
using AuraEcho.ExternalTools.Models;
using AuraEcho.ExternalTools.Utils;
using AuraEcho.PluginContracts.Constants;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;

namespace AuraEcho.ExternalTools.ViewModels;

public class EditExternalToolViewModel : BindableBase, INavigationAware
{
    #region private members
    private ExternalTool _editingExternalTool;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;
    private string _name;
    private string _command;
    private string _arguments;
    #endregion

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    public string Command
    {
        get => _command;
        set => SetProperty(ref _command, value);
    }
    public string Arguments
    {
        get => _arguments;
        set => SetProperty(ref _arguments, value);
    }

    public DelegateCommand SubmitCommand { get; }
    private void Submit()
    {
        var newExternalTool = new Models.ExternalTool
        {
            Id = _editingExternalTool.Id,
            Name = Name,
            Command = FixCommand(Command),
            Arguments = Arguments
        };
        newExternalTool.Type = ShellHelper.CheckExternalToolType(newExternalTool.Command);


        _eventAggregator.GetEvent<ExternalToolUpdatedEvent>().Publish(newExternalTool);
        _regionManager.Regions[HostRegionNames.DialogRegion].RemoveAll();
    }

    private string FixCommand(string command)
    {
        var trimed = Command.Trim('"');
        if (trimed.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            return "http://" + trimed;
        }
        return trimed;
    }
    public bool CanSubmit() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Command);

    public DelegateCommand CancelCommand { get; }
    private void Cancel()
    {
        _regionManager.Regions[HostRegionNames.DialogRegion].RemoveAll();
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _editingExternalTool = navigationContext.Parameters["ExternalTool"] as ExternalTool;
        Name = _editingExternalTool.Name;
        Command = _editingExternalTool.Command;
        Arguments = _editingExternalTool.Arguments;

    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
        => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    public EditExternalToolViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
    {
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;

        SubmitCommand = new DelegateCommand(Submit, CanSubmit).ObservesProperty(() => Name).ObservesProperty(() => Command);
        CancelCommand = new DelegateCommand(Cancel);
    }
}
