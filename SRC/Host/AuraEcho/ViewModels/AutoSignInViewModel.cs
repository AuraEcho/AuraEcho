using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Auth;
using AuraEcho.Cloud.V1.Models.Common;
using AuraEcho.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Services;
using AuraEcho.Strings;
using AuraEcho.PluginContracts.Constants;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuraEcho.ViewModels;

public class AutoSignInViewModel : BindableBase, IRegionMemberLifetime
{
    private readonly INavigationService _navigationService;
    private readonly IClientSession _clientSession;
    private readonly ITokenProvider _tokenProvider;
    private readonly IAuraToastService _auraToastService;
    private readonly ApiClient _apiClient;
    private readonly CancellationTokenSource _cts = new();

    public DelegateCommand CancelSignInCommand { get; }
    private void CancelSignIn()
    {
        _cts.Cancel();
        _tokenProvider.ClearToken();
        _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.SignIn);
    }

    public AutoSignInViewModel(
        ITokenProvider tokenProvider, 
        ApiClient apiClient, 
        IClientSession clientSession, 
        INavigationService navigationService,
        IAuraToastService auraToastService)
    {
        _tokenProvider = tokenProvider;
        _apiClient = apiClient;
        _auraToastService = auraToastService;
        _clientSession = clientSession;
        _navigationService = navigationService;

        CancelSignInCommand = new DelegateCommand(CancelSignIn);

        SignInByRefreshToken(_cts.Token);
    }

    private async void SignInByRefreshToken(CancellationToken ct)
    {
        if (String.IsNullOrWhiteSpace(_tokenProvider.RefreshToken))
        {
            _navigationService.RequestNavigate( HostRegionNames.MainRegion, ViewNames.SignIn, canBack: false);
            return;
        }

        var refreshTokenTask = _apiClient.Auth.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = _tokenProvider.RefreshToken
        });

        await Task.WhenAll(
            Task.Delay(TimeSpan.FromSeconds(2)),
            refreshTokenTask);

        if (ct.IsCancellationRequested) return;

        ResponseResult<AuthResponse>? result = refreshTokenTask.Result;
        if (result is null || result.Data is null)
        {
            _tokenProvider.ClearToken();
            _auraToastService.Show(Labels.AutoSignIn_SignInExpired);
            _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.SignIn, canBack: false);
            return;
        }

        _clientSession.SignIn(result.Data);

        _navigationService.RequestNavigate(HostRegionNames.MainRegion, ViewNames.Homepage);
    }

    public bool KeepAlive => false;
}
