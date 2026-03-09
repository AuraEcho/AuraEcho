using Prism.Commands;
using Prism.Mvvm;

namespace AuraEcho.Setup.UI.ViewModels;

public class ActionCancelledViewModel : BindableBase
{
    public DelegateCommand FinishedCommand { get; }
    private void Finished()
    {
        App.Current.Shutdown();
    }

    public ActionCancelledViewModel()
    {
        FinishedCommand = new DelegateCommand(Finished);
    }
}
