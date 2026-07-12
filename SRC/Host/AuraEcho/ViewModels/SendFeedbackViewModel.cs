using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Diagnostics;

namespace AuraEcho.ViewModels;

public class SendFeedbackViewModel : BindableBase, IRegionMemberLifetime
{
    public bool KeepAlive => false;

    public DelegateCommand<string> OpenUrlCommand { get; }
    private void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    public SendFeedbackViewModel()
    {
        OpenUrlCommand = new DelegateCommand<string>(OpenUrl);
    }
}
