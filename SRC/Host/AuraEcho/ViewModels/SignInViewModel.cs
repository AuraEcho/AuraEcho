using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Auth;
using AuraEcho.Cloud.V1.Models.Common;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Strings;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public partial class SignInViewModel : BindableBase, INotifyDataErrorInfo, IRegionMemberLifetime
{
    private readonly INavigationService _navigationService;
    private readonly ApiClient _apiClient;
    private readonly IClientSession _clientSession;
    private readonly IAuraToastService _toastService;
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool IsBusy
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string Email
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string Password
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string EmailCode
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int SendEmailCodeCooldown
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand SendEmailCodeCommand { get; }
    private async void SendEmailCode()
    {
        ClearErrors(nameof(Email));
        ClearErrors(nameof(EmailCode));
        if (ValidateCore(nameof(Email)) is string emailError && emailError != String.Empty)
        {
            _errors[nameof(Email)] = [emailError];
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
            return;
        }

        SendEmailCodeCooldown = 60;
        ResponseResult<string> requestResult =
            await _apiClient.Auth.SendEmailVerificationCodeAsync(
                new SendEmailCodeRequest(
                    Email.Trim(),
                    EmailCodeScene.SignIn));

        if (requestResult is null || requestResult.Status != ResultStatus.Success)
        {
            SendEmailCodeCooldown = 0;
            _toastService.Show(Labels.SignIn_CodeSendFailed, ToastLevel.Error);
            return;
        }

        _toastService.Show(string.Format(Labels.SignIn_CodeSent, Email), ToastLevel.Info);

        _ = Task.Run(async () =>
        {
            SendEmailCodeCooldown = 60;
            while (SendEmailCodeCooldown > 0)
            {
                await Task.Delay(1000);
                SendEmailCodeCooldown--;
            }
        });
    }

    public DelegateCommand SignInByCodeCommand { get; }
    private async void SignInByCode()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            ClearErrors(nameof(Email));
            ClearErrors(nameof(EmailCode));
            if (ValidateCore(nameof(Email)) is string emailError && emailError != String.Empty)
            {
                _errors[nameof(Email)] = [emailError];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
                return;
            }

            if (ValidateCore(nameof(EmailCode)) is string emailCodeError && emailCodeError != String.Empty)
            {
                _errors[nameof(EmailCode)] = [emailCodeError];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(EmailCode)));
                return;
            }

            ResponseResult<CodeSignInResponse>? result =
                await _apiClient.Auth.SignInByCodeAsync(new CodeSignInRequest(Email.Trim(), EmailCode.Trim()));

            if (result?.Status == ResultStatus.EmailCodeError)
            {
                _errors[nameof(EmailCode)] = [Labels.SignIn_CodeError];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(EmailCode)));
                return;
            }

            if (result is null || result.Status != ResultStatus.Success || result.Data is null)
            {
                _toastService.Show(Labels.SignIn_ServerBusy, ToastLevel.Error);
                return;
            }

            _clientSession.SignIn(result.Data.Data);
            _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.Homepage);
        }
        finally
        {
            IsBusy = false;
        }
    }
    public DelegateCommand<string> ClearErrorsCommand { get; }
    private void ClearErrors(string propertyName)
    {
        if (_errors.ContainsKey(propertyName))
        {
            _errors.Remove(propertyName);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }

    private static readonly Regex EmailRegex = BuildEmailRegex();
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "zh-CN")]
    private static partial Regex BuildEmailRegex();

    private string ValidateCore(string propertyName) => propertyName switch
    {
        nameof(Email) when String.IsNullOrWhiteSpace(Email) => Labels.SignIn_EmailRequired,
        nameof(Email) when EmailRegex.IsMatch(Email) == false => Labels.SignIn_EmailInvalidFormat,
        nameof(EmailCode) when String.IsNullOrWhiteSpace(EmailCode) => Labels.SignIn_CodeRequired,
        nameof(Password) when String.IsNullOrWhiteSpace(Password) => Labels.SignIn_PasswordRequired,
        _ => String.Empty
    };

    public DelegateCommand SignInByPasswordCommand { get; }
    private async void SignInByPassword()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            ClearErrors(nameof(Email));
            ClearErrors(nameof(Password));
            if (ValidateCore(nameof(Email)) is string emailError && emailError != String.Empty)
            {
                _errors[nameof(Email)] = [emailError];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
                return;
            }

            if (ValidateCore(nameof(Password)) is string passwordError && passwordError != String.Empty)
            {
                _errors[nameof(Password)] = [passwordError];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Password)));
                return;
            }

            ResponseResult<AuthResponse>? result =
                await _apiClient.Auth.SignInByPasswordAsync(new PasswordSignInRequest(Email.Trim(), Password.Trim()));

            if (result?.Status == ResultStatus.PasswordError)
            {
                _errors[nameof(Email)] = [Labels.SignIn_AccountOrPasswordError];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
                return;
            }

            if (result is null || result.Status != ResultStatus.Success || result.Data is null)
            {
                _toastService.Show(Labels.SignIn_ServerBusy, ToastLevel.Error);
                return;
            }

            _clientSession.SignIn(result.Data);
            _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.Homepage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public DelegateCommand NavigationToResetPasswordCommand { get; }
    private void NavigationToResetPassword()
    {
        _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.ResetPassword, null, false);
    }

    public DelegateCommand ResetDataCommand { get; set; }
    private void ResetData()
    {
        Email = String.Empty;
        EmailCode = String.Empty;
        Password = String.Empty;

        ClearErrors(nameof(Email));
        ClearErrors(nameof(EmailCode));
        ClearErrors(nameof(Password));
    }

    #region INotifyDataErrorInfo Implementation
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public IEnumerable GetErrors(string? propertyName)
    {
        return _errors.TryGetValue(propertyName, out List<string>? value) ? value : null;
    }
    public bool HasErrors => _errors.Count != 0;

    #endregion 
    public bool KeepAlive => false;

    public SignInViewModel(
        INavigationService navigationService,
        ApiClient apiClient,
        IClientSession clientSession,
        IAuraToastService auraToastService)
    {
        _navigationService = navigationService;
        _apiClient = apiClient;
        _clientSession = clientSession;
        _toastService = auraToastService;

        SendEmailCodeCommand = new DelegateCommand(SendEmailCode);
        SignInByCodeCommand = new DelegateCommand(SignInByCode);
        SignInByPasswordCommand = new DelegateCommand(SignInByPassword);
        ResetDataCommand = new DelegateCommand(ResetData);
        NavigationToResetPasswordCommand = new DelegateCommand(NavigationToResetPassword);
        ClearErrorsCommand = new DelegateCommand<string>(ClearErrors);
    }
}
