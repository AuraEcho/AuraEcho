using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Models.Api;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AuraEcho.ViewModels;

public partial class SignInViewModel : BindableBase, INotifyDataErrorInfo, IRegionMemberLifetime
{
    private readonly IRegionManager _regionManager;
    private readonly IAuthRepository _authRepository;
    private readonly IClientSession _clientSession;
    private readonly Dictionary<string, List<string>> _errors = [];

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
        SendEmailCodeCooldown = 60;
        bool requestResult = await _authRepository.SendEmailVerificationCodeAsync(Email.Trim());

        if (!requestResult)
        {
            SendEmailCodeCooldown = 0;
            // TODO: 显示发送失败的提示
            return;
        }

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
            await _authRepository.SignInByCodeAsync(new CodeSignInRequest(Email.Trim(), EmailCode.Trim()));

        if (result.Status == ResultStatus.EmailCodeError)
        {
            _errors[nameof(EmailCode)] = ["验证码错误"];
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(EmailCode)));
            return;
        }

        if (result.Status != ResultStatus.Success || result.Data is null)
        {
            // TODO: 提示失败
            return;
        }

        _clientSession.SignIn(new AppToken
        {
            AccessToken = result.Data.Data.AccessToken,
            RefreshToken = result.Data.Data.RefreshToken,
            ExpiresAt = result.Data.Data.ExpiresAt
        });
        _regionManager.RequestNavigate(HostRegionNames.HomeRegion, nameof(Homepage));
    }

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
        nameof(Email) when String.IsNullOrWhiteSpace(Email) => "邮箱地址不能为空！",
        nameof(Email) when EmailRegex.IsMatch(Email) == false => "请输入有效的邮箱格式",
        nameof(EmailCode) when String.IsNullOrWhiteSpace(EmailCode) => "验证码不能为空！",
        nameof(Password) when String.IsNullOrWhiteSpace(Password) => "密码不能为空！",
        _ => String.Empty
    };

    public DelegateCommand SignInByPasswordCommand { get; }
    private async void SignInByPassword()
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
            await _authRepository.SignInByPasswordAsync(new PasswordSignInRequest(Email.Trim(), Password.Trim()));

        if (result.Status == ResultStatus.PasswordError)
        {
            _errors[nameof(Email)] = ["账号或密码错误"];
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
            return;
        }

        if (result.Status != ResultStatus.Success || result.Data is null)
        {
            // TODO: 提示失败
            return;
        }

        _clientSession.SignIn(new AppToken
        {
            AccessToken = result.Data.AccessToken,
            RefreshToken = result.Data.RefreshToken,
            ExpiresAt = result.Data.ExpiresAt
        });
        _regionManager.RequestNavigate(HostRegionNames.HomeRegion, nameof(Homepage));
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
    /// <summary>
    /// 打开协议声明
    /// </summary>
    public DelegateCommand OpenEULACommand { get; }
    private void OpenEULA()
    {
        string currentFolderPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        string filePath = Path.Combine(currentFolderPath, "Assets/PDF/EULA.pdf");

        Task.Run(() =>
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = filePath
            }));
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


    public SignInViewModel(IRegionManager regionManager, IAuthRepository authRepository, IClientSession clientSession)
    {
        _regionManager = regionManager;
        _authRepository = authRepository;
        _clientSession = clientSession;

        SendEmailCodeCommand = new DelegateCommand(SendEmailCode);
        SignInByCodeCommand = new DelegateCommand(SignInByCode);
        SignInByPasswordCommand = new DelegateCommand(SignInByPassword);
        OpenEULACommand = new DelegateCommand(OpenEULA);
        ResetDataCommand = new DelegateCommand(ResetData);
    }
}
