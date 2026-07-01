using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AuraEcho.Api.Models.V1.Auth;
using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Strings;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public partial class AccountSettingsViewModel : BindableBase, INotifyDataErrorInfo, IRegionMemberLifetime
{
    #region private members
    private readonly IClientSession _clientSession;
    private readonly IAuthRepository _authRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IAuraToastService _toastService;
    private readonly Dictionary<string, List<string>> _errors = [];
    #endregion

    public bool IsSubmitting
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsUploadingAvatar
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
        => !IsSubmitting && HasChanges() && !IsUploadingAvatar;

    private async void UpdateProfile()
    {
        IsSubmitting = true;
        try
        {
            ClearErrors(nameof(NewUserName));
            if (ValidateCore(nameof(NewUserName)) is string userNameError && userNameError != String.Empty)
            {
                _errors[nameof(NewUserName)] = [userNameError];
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(NewUserName)));
                return;
            }

            var updateProfileTask = _authRepository.UpdateProfileAsync(new UpdateProfileRequest
            {
                AvatarFileId = NewAvatarFileId,
                UserName = NewUserName
            });
            await Task.WhenAll(updateProfileTask, Task.Delay(TimeSpan.FromSeconds(0.3)));

            if (updateProfileTask.Result.Status != ResultStatus.Success)
            {
                _toastService.Show(Labels.AccountSettings_ProfileUpdateFailed, ToastLevel.Error);
                return;
            }

            var userInfo = await _authRepository.GetCurrentUserAsync();
            _clientSession.UpdateUserProfile(userInfo.ToUserProfile());
            _toastService.Show(Labels.AccountSettings_ProfileUpdateSucceeded, ToastLevel.Success);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public DelegateCommand<string> UploadAvatarCommand { get; }
    private async void UploadAvatar(string filePath)
    {
        if (IsUploadingAvatar) return;

        IsUploadingAvatar = true;
        try
        {
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Length > 2 * 1024 * 1024)
            {
                _toastService.Show(Labels.AccountSettings_AvatarSizeExceeded, ToastLevel.Error);
                return;
            }

            var uploadResult = await _storageRepository.UploadFileAsync(filePath);
            if (uploadResult is null)
            {
                _toastService.Show(Labels.AccountSettings_AvatarUploadFailed, ToastLevel.Error);
                return;
            }

            NewAvatarFileUrl = uploadResult.FileUrl;
            NewAvatarFileId = uploadResult.FileId;
        }
        finally
        {
            IsUploadingAvatar = false;
        }
    }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasErrors => _errors.Count != 0;
    public DelegateCommand<string> ClearErrorsCommand { get; }

    public bool KeepAlive => false;

    private void ClearErrors(string propertyName)
    {
        if (!_errors.Remove(propertyName)) return;

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
    private string ValidateCore(string propertyName) => propertyName switch
    {
        nameof(NewUserName) when String.IsNullOrWhiteSpace(NewUserName) => Labels.AccountSettings_UserNameRequired,
        nameof(NewUserName) when NewUserName.Contains(' ') => Labels.AccountSettings_UserNameNoSpaces,
        nameof(NewUserName) when NewUserName.Length < 4 || NewUserName.Length > 16 => Labels.AccountSettings_UserNameLengthInvalid,
        nameof(NewUserName) when !UsernameRegex().IsMatch(NewUserName) => Labels.AccountSettings_UserNameNoSpecialChars,
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
            .ObservesProperty(() => IsSubmitting)
            .ObservesProperty(() => IsUploadingAvatar)
            .ObservesProperty(() => NewAvatarFileId)
            .ObservesProperty(() => NewUserName);

        UploadAvatarCommand = new DelegateCommand<string>(UploadAvatar);
        ClearErrorsCommand = new DelegateCommand<string>(ClearErrors);
        NewUserName = _clientSession.CurrentUser.UserName;
        NewAvatarFileId = _clientSession.CurrentUser.AvatarFileId;
        NewAvatarFileUrl = _clientSession.CurrentUser.AvatarFileUrl;
    }
}
