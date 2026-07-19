using AuraEcho.PluginContracts.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Collections.Generic;
using System.Diagnostics;

namespace AuraEcho.ViewModels;

public class SendFeedbackViewModel : BindableBase, IRegionMemberLifetime
{
    private readonly ITelemetryService _telemetry;

    public bool KeepAlive => false;

    public DelegateCommand<string> OpenUrlCommand { get; }
    private void OpenUrl(string url)
    {
        _telemetry.TrackEvent("Feedback.LinkClicked", new Dictionary<string, string>
        {
            ["url"] = url
        });

        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    public SendFeedbackViewModel(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
        OpenUrlCommand = new DelegateCommand<string>(OpenUrl);
    }
}
