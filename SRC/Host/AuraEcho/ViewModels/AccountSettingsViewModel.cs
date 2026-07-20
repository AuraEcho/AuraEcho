using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Auth;
using AuraEcho.Cloud.V1.Models.Common;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Extensions;
using AuraEcho.Strings;
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
    private readonly ApiClient _apiClient;
    private readonly IAuraToastService _toastService;
    private readonly ITelemetryService _telemetry;
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

            var updateProfileTask = _apiClient.Auth.UpdateProfileAsync(new UpdateProfileRequest
            {
                AvatarFileId = NewAvatarFileId,
                UserName = NewUserName
            });
            await Task.WhenAll(updateProfileTask, Task.Delay(TimeSpan.FromSeconds(0.3)));

            if (updateProfileTask.Result.Status != ResultStatus.Success)
            {
                _telemetry.TrackEvent("Account.ProfileUpdateFailed", new Dictionary<string, string>
                {
                    ["status"] = updateProfileTask.Result.Status.ToString()
                });
                _toastService.Show(Labels.AccountSettings_ProfileUpdateFailed, ToastLevel.Error);
                return;
            }

            var userInfo = await _apiClient.Auth.GetCurrentUserAsync();
            _clientSession.UpdateUserProfile(userInfo.ToUserProfile());
            _telemetry.TrackEvent("Account.ProfileUpdated");
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
                _telemetry.TrackEvent("Account.AvatarUploadRejected", new Dictionary<string, string>
                {
                    ["reason"] = "sizeExceeded"
                });
                _toastService.Show(Labels.AccountSettings_AvatarSizeExceeded, ToastLevel.Error);
                return;
            }

            var uploadResult = await _apiClient.File.UploadFileAsync(filePath);
            if (uploadResult is null)
            {
                _telemetry.TrackEvent("Account.AvatarUploadFailed");
                _toastService.Show(Labels.AccountSettings_AvatarUploadFailed, ToastLevel.Error);
                return;
            }

            NewAvatarFileUrl = uploadResult.FileUrl;
            NewAvatarFileId = uploadResult.FileId;
            _telemetry.TrackEvent("Account.AvatarUploaded");
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

    public AccountSettingsViewModel(IClientSession clientSession, ApiClient apiClient, IAuraToastService toastService, ITelemetryService telemetry)
    {
        _clientSession = clientSession;
        _apiClient = apiClient;
        _toastService = toastService;
        _telemetry = telemetry;

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
