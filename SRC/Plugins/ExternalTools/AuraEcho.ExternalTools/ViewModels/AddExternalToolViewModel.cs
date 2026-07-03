using AuraEcho.ExternalTools.Events;
using AuraEcho.ExternalTools.Utils;
using AuraEcho.PluginContracts.Constants;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;

namespace AuraEcho.ExternalTools.ViewModels;

public class AddExternalToolViewModel : BindableBase
{
    #region private members
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
            Id = Guid.NewGuid().ToString(),
            Name = Name,
            Command = FixCommand(Command),
            Arguments = Arguments
        };
        newExternalTool.Type = ShellHelper.CheckExternalToolType(newExternalTool.Command);

        _eventAggregator.GetEvent<ExternalToolAddedEvent>().Publish(newExternalTool);
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

    public AddExternalToolViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
    {
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;

        SubmitCommand = new DelegateCommand(Submit, CanSubmit).ObservesProperty(() => Name).ObservesProperty(() => Command);
        CancelCommand = new DelegateCommand(Cancel);
    }
}
