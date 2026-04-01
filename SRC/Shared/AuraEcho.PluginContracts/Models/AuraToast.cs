using Prism.Mvvm;

namespace AuraEcho.PluginContracts.Models
{
    public class AuraToast : BindableBase
    {
        public string Message { get; set; }

        private bool _isClosing;
        public bool IsClosing
        {
            get => _isClosing;
            set => SetProperty(ref _isClosing, value);
        }
        public ToastLevel Level { get; set; }
    }

}