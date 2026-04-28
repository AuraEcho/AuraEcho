using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Enums;
using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Models.Api.Auth;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public partial class ResetPasswordViewModel : BindableBase, INotifyDataErrorInfo, IRegionMemberLifetime
{
    private readonly INavigationService _navigationService;
    private readonly IAuthRepository _authRepository;
    private readonly IAuraToastService _toastService;
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool IsSubmitting
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string Email
    {
        get;
        set => SetProperty(ref field, value);
    }

    private string _password;
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
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
            await _authRepository.SendEmailVerificationCodeAsync(
                new SendEmailCodeRequest(
                    Email.Trim(), 
                    EmailCodeScene.ResetPassword));
        if (requestResult.Status == ResultStatus.UserNotFound)
        {
            SendEmailCodeCooldown = 0;
            _toastService.Show($"用户不存在，请检查邮箱地址是否正确。", ToastLevel.Error);
            return;
        }

        if (requestResult.Status != ResultStatus.Success)
        {
            SendEmailCodeCooldown = 0;
            _toastService.Show($"服务器繁忙，请稍后重试。", ToastLevel.Error);
            return;
        }

        _toastService.Show($"验证码已发送至 {Email}", ToastLevel.Info);
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


    public DelegateCommand ResetPasswordCommand { get; }
    private async void ResetPassword()
    {
        IsSubmitting = true;

        ClearErrors(nameof(Email));
        ClearErrors(nameof(EmailCode));
        ClearErrors(nameof(Password));

        if (ValidateCore(nameof(Email)) is string emailError && emailError != String.Empty)
        {
            _errors[nameof(Email)] = [emailError];
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
            IsSubmitting = false;
            return;
        }

        if (ValidateCore(nameof(EmailCode)) is string emailCodeError && emailCodeError != String.Empty)
        {
            _errors[nameof(EmailCode)] = [emailCodeError];
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(EmailCode)));
            IsSubmitting = false;
            return;
        }

        if (ValidateCore(nameof(Password)) is string passwordError && passwordError != String.Empty)
        {
            _errors[nameof(Password)] = [passwordError];
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Password)));
            IsSubmitting = false;
            return;
        }

        ResponseResult<string>? result =
            await _authRepository.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = Email,
                EmailCode = EmailCode,
                NewPassword = Password,
            });

        if (result?.Status == ResultStatus.UserNotFound)
        {
            _errors[nameof(Email)] = ["用户不存在"];
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
            IsSubmitting = false;
            return;
        }

        if (result?.Status == ResultStatus.EmailCodeError)
        {
            _errors[nameof(EmailCode)] = ["验证码错误"];
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(EmailCode)));
            IsSubmitting = false;
            return;
        }

        if (result is null || result.Status != ResultStatus.Success || result.Data is null)
        {
            _toastService.Show($"服务器繁忙，请稍后重试。", ToastLevel.Error);
            IsSubmitting = false;
            return;
        }
        IsSubmitting = false;
        _navigationService.RequestNavigate(HostRegionNames.HomeRegion, ViewNames.PasswordResetCompleted, null, false);
    }

    public DelegateCommand BackToSignInCommand { get; }
    private void BackToSignIn()
    {
        _navigationService.RequestNavigate(HostRegionNames.HomeRegion, ViewNames.SignIn, null, false);
    }

    public DelegateCommand<string> ClearErrorsCommand { get; }
    private void ClearErrors(string propertyName)
    {
        if (_errors.ContainsKey(propertyName))
        {
            _errors.Remove(propertyName);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            RaisePropertyChanged(nameof(HasErrors));
        }
    }

    private static readonly Regex EmailRegex = BuildEmailRegex();
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "zh-CN")]
    private static partial Regex BuildEmailRegex();

    private string ValidateCore(string propertyName) => propertyName switch
    {
        nameof(Email) when String.IsNullOrWhiteSpace(Email) => "邮箱地址不能为空！",
        nameof(Email) when EmailRegex.IsMatch(Email) == false => "请输入有效的邮箱格式",
        nameof(EmailCode) when String.IsNullOrWhiteSpace(EmailCode) => "验证码不能为空！",
        nameof(Password) when String.IsNullOrWhiteSpace(Password) => "密码不能为空！",
        nameof(Password) when Password.Any(Char.IsWhiteSpace) => "密码不能包含空格！",
        nameof(Password) when Password.Length < 8 => "密码长度过短！",
        nameof(Password) when !Password.Any(Char.IsLetter) || !Password.Any(Char.IsDigit) => "密码必须同时包含字母和数字！",
        _ => String.Empty
    };

    #region INotifyDataErrorInfo Implementation
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public IEnumerable GetErrors(string? propertyName)
    {
        return _errors.TryGetValue(propertyName, out List<string>? value) ? value : null;
    }
    public bool HasErrors => _errors.Count != 0;

    #endregion
    public bool KeepAlive => false;


    public ResetPasswordViewModel(INavigationService navigationService, IAuthRepository authRepository, IAuraToastService auraToastService)
    {
        _toastService = auraToastService;
        _navigationService = navigationService;
        _authRepository = authRepository;

        SendEmailCodeCommand = new DelegateCommand(SendEmailCode);
        ResetPasswordCommand = new DelegateCommand(ResetPassword);
        BackToSignInCommand = new DelegateCommand(BackToSignIn);
        ClearErrorsCommand = new DelegateCommand<string>(ClearErrors);
    }
}
