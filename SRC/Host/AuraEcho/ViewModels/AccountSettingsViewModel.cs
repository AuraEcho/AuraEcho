using AuraEcho.Api.Models.V1.Auth;
using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Enums;
using AuraEcho.Core.Extensions;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

namespace AuraEcho.ViewModels;

public partial class AccountSettingsViewModel : BindableBase, INotifyDataErrorInfo
{
    #region private members
    private readonly IClientSession _clientSession;
    private readonly IAuthRepository _authRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IAuraToastService _toastService;
    private readonly Dictionary<string, List<string>> _errors = [];
    #endregion

    public bool IsBusy
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string NewUserName
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string NewAvatarFileUrl
    {
        get;
        set => SetProperty(ref field, value);
    }
    public Guid? NewAvatarFileId
    {
        get;
        set => SetProperty(ref field, value);
    }

    private bool HasChanges()
        => NewUserName != _clientSession.CurrentUser.UserName ||
           NewAvatarFileId != _clientSession.CurrentUser.AvatarFileId;

    public DelegateCommand UpdateProfileCommand { get; }
    public bool CanUpdateProfile()
        => !IsBusy && HasChanges();

    private async void UpdateProfile()
    {
        IsBusy = true;
        try
        {
            ClearErrors(nameof(NewUserName));
            if (ValidateCore(nameof(NewUserName)) is string userNameError && userNameError != String.Empty)
            {
                _errors[nameof(NewUserName)] = [userNameError];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(NewUserName)));
                return;
            }

            var result = await _authRepository.UpdateProfileAsync(new UpdateProfileRequest
            {
                AvatarFileId = NewAvatarFileId,
                UserName = NewUserName
            });

            if (result.Status != ResultStatus.Success)
            {
                _toastService.Show("个人信息更新失败", ToastLevel.Error);
                return;
            }

            var userInfo = await _authRepository.GetCurrentUserAsync();
            _clientSession.UpdateUserProfile(userInfo.ToUserProfile());
            _toastService.Show("个人信息更新成功", ToastLevel.Success);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public DelegateCommand<string> UploadAvatarCommand { get; }
    private async void UploadAvatar(string filePath)
    {
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length > 2 * 1024 * 1024) throw new Exception("图像大小不能超过2MB");

        var uploadResult = await _storageRepository.UploadFileAsync(filePath);
        if (uploadResult is null)
        {
            _toastService.Show("上传头像失败", ToastLevel.Error);
            return;
        }

        NewAvatarFileUrl = uploadResult.FileUrl;
        NewAvatarFileId = uploadResult.FileId;
    }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasErrors => _errors.Count != 0;
    public DelegateCommand<string> ClearErrorsCommand { get; }
    private void ClearErrors(string propertyName)
    {
        if (!_errors.Remove(propertyName)) return;

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
    private string ValidateCore(string propertyName) => propertyName switch
    {
        nameof(NewUserName) when String.IsNullOrWhiteSpace(NewUserName) => "用户名不能为空",
        nameof(NewUserName) when NewUserName.Contains(' ') => "用户名不能包含空格",
        nameof(NewUserName) when NewUserName.Length < 4 || NewUserName.Length > 16 => "用户名长度应在4-16个字符之间",
        nameof(NewUserName) when !UsernameRegex().IsMatch(NewUserName) => "用户名不能包含特殊字符",
        _ => String.Empty
    };

    [GeneratedRegex(@"^[\p{L}0-9]+$")]
    private static partial Regex UsernameRegex();

    public IEnumerable GetErrors(string? propertyName)
    {
        return _errors.TryGetValue(propertyName, out List<string>? value) ? value : null;
    }

    public AccountSettingsViewModel(IClientSession clientSession, IAuthRepository authRepository, IStorageRepository fileRepository, IAuraToastService toastService)
    {
        _clientSession = clientSession;
        _authRepository = authRepository;
        _storageRepository = fileRepository;
        _toastService = toastService;

        UpdateProfileCommand =
            new DelegateCommand(UpdateProfile, CanUpdateProfile)
            .ObservesProperty(() => IsBusy)
            .ObservesProperty(() => NewAvatarFileId)
            .ObservesProperty(() => NewUserName);

        UploadAvatarCommand = new DelegateCommand<string>(UploadAvatar);
        ClearErrorsCommand = new DelegateCommand<string>(ClearErrors);
        NewUserName = _clientSession.CurrentUser.UserName;
        NewAvatarFileId = _clientSession.CurrentUser.AvatarFileId;
        NewAvatarFileUrl = _clientSession.CurrentUser.AvatarFileUrl;
    }
}
