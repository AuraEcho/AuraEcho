using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Services;

public class AuraToastService : IAuraToastService
{
    public ObservableCollection<AuraToast> ActiveToasts { get; } = [];

    public async void Show(string message, ToastLevel level = ToastLevel.Info, double duration = 3)
    {
        var toast = new AuraToast
        {
            Message = message,
            Level = level
        };

        ActiveToasts.Add(toast);

        await Task.Delay(TimeSpan.FromSeconds(duration));

        toast.IsClosing = true;

        await Task.Delay(TimeSpan.FromSeconds(0.3));

        ActiveToasts.Remove(toast);
    }
}
