using System;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace AuraEcho.UIToolkit.RegionDialog
{
    public class ConfirmDialogViewModel : BindableBase, IRegionDialogAware
    {
        private RegionDialogParameter _parameter;
        public RegionDialogParameter Parameter
        {
            get => _parameter;
            set => SetProperty(ref _parameter, value);
        }

        public DelegateCommand OkCommand { get; }
        private void Ok()
        {
            RequestClose?.Invoke(RegionDialogResult.OK);
        }

        public DelegateCommand CancelCommand { get; }
        private void Cancel()
        {
            RequestClose?.Invoke(RegionDialogResult.Cancel);
        }
        public DelegateCommand CloseCommand { get; }
        private void Close()
        {
            RequestClose?.Invoke(RegionDialogResult.Close);
        }

        public event Action<RegionDialogResult> RequestClose;

        public ConfirmDialogViewModel()
        {
            OkCommand = new DelegateCommand(Ok);
            CancelCommand = new DelegateCommand(Cancel);
            CloseCommand = new DelegateCommand(Close);
        }

        public void OnDialogOpened(RegionDialogParameter parameters)
        {
            Parameter = parameters;
        }
    }
}
