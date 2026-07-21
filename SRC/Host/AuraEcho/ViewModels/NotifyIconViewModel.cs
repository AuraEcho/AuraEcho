using AuraEcho.Telemetry;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.PluginContracts.Events;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Commands;
using Prism.DryIoc;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class NotifyIconViewModel : BindableBase
{
    #region private
    private readonly IEventAggregator _eventAggregator;
    private readonly ITelemetryService _telemetry;
    #endregion

    public bool IsSignedIn
    {
        get => field;
        set => SetProperty(ref field, value);
    } = false;

    public ICommand ShowWindowCommand { get; }
    private void ShowWindow()
    {
        _telemetry?.TrackEvent("NotifyIcon.ShowWindow");
        _eventAggregator.GetEvent<RequestShowAppEvent>().Publish();
    }

    public ICommand ExitApplicationCommand { get; }
    private void ExitApplication()
    {
        _telemetry?.TrackEvent("NotifyIcon.Exit");
        _eventAggregator.GetEvent<AppShutdownEvent>().Publish();
    }

    public DelegateCommand<string> GoToTargetViewCommand { get; }
    private void GoToTargetView(string viewName)
    {
        if (!IsSignedIn) return;

        _telemetry?.TrackEvent("NotifyIcon.GoToView", new System.Collections.Generic.Dictionary<string, string>
        {
            ["view"] = viewName ?? string.Empty
        });
        _eventAggregator.GetEvent<RequestViewEvent>().Publish(viewName);
        ShowWindow();
    }

    public NotifyIconViewModel()
    {
        ExitApplicationCommand = new DelegateCommand(ExitApplication);
        ShowWindowCommand = new DelegateCommand(ShowWindow);
        GoToTargetViewCommand = new DelegateCommand<string>(GoToTargetView);
             
        var container = (Application.Current as PrismApplication)!.Container;
        _eventAggregator = (container.Resolve(typeof(IEventAggregator)) as IEventAggregator)!;
        _telemetry = container.Resolve(typeof(ITelemetryService)) as ITelemetryService;
        _eventAggregator.GetEvent<SignedInEvent>().Subscribe(OnSignedIn, ThreadOption.UIThread);
        _eventAggregator.GetEvent<SignedOutEvent>().Subscribe(OnSignedOut, ThreadOption.UIThread);
    }

    private void OnSignedIn() => IsSignedIn = true;
    private void OnSignedOut() => IsSignedIn = false;
}
