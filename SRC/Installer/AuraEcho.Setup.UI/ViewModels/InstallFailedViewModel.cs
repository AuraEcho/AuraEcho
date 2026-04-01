using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.Setup.UI.ViewModels
{
    public class InstallFailedViewModel : BindableBase, INavigationAware
    {
        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public DelegateCommand FinishedCommand { get; }
        private void Finished()
        {
            App.Current.Shutdown();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
            => Message = navigationContext.Parameters.GetValue<string>("Message");

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        public InstallFailedViewModel()
        {
            FinishedCommand = new DelegateCommand(Finished);
        }
    }
}
