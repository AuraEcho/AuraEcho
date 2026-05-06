using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Enums;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using AuraEcho.Core.Models.Api.Auth;
using AuraEcho.Core.Repositories;
using AuraEcho.PluginContracts.Interfaces;
using AuraEcho.PluginContracts.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace AuraEcho.ViewModels;

public class AccountSettingsViewModel : BindableBase
{
    #region private members
    private readonly IClientSession _clientSession;
    private readonly IAuthRepository _authRepository;
    private readonly IFileRepository _fileRepository;
    private readonly IAuraToastService _toastService;
    #endregion

    public bool IsBusy
    {
        get;
        set => SetProperty(ref field, value);
    }

    public UserProfile NewUserProfile
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand UpdateProfileCommand { get; }
    public bool CanUpdateProfile() => !IsBusy;
    private async void UpdateProfile()
    {
        IsBusy = true;
        try
        {
            var result = await _authRepository.UpdateProfileAsync(new UpdateProfileRequest
            {
                AvatarFileId = NewUserProfile.AvatarFileId,
                UserName = NewUserProfile.UserName
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

        if (fileInfo.Length > 5 * 1024 * 1024) throw new Exception("文件大小不能超过5MB");

        NewUserProfile.AvatarFileId = await _fileRepository.UploadFileAsync(filePath, "image")
            ?? throw new Exception("上传文件时出错");
    }

    public AccountSettingsViewModel(IClientSession clientSession, IAuthRepository authRepository, IFileRepository fileRepository, IAuraToastService toastService)
    {
        _clientSession = clientSession;
        _authRepository = authRepository;
        _fileRepository = fileRepository;
        _toastService = toastService;

        UpdateProfileCommand = new DelegateCommand(UpdateProfile, CanUpdateProfile).ObservesProperty(() => IsBusy);
        UploadAvatarCommand = new DelegateCommand<string>(UploadAvatar);

        NewUserProfile = new UserProfile
        {
            UserName = _clientSession.CurrentUser.UserName,
            Email = _clientSession.CurrentUser.Email,
            AvatarFileId = _clientSession.CurrentUser.AvatarFileId
        };
    }
}
