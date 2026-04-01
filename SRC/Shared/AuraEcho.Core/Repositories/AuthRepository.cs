using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Models.Api.Auth;
using AuraEcho.Core.Tools;
using System.Net.Http.Json;

namespace AuraEcho.Core.Repositories;

public class AuthRepository : IAuthRepository
{
    private HttpHelper _httpHelper;

    public AuthRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }

    public async Task<AppUserDto> GetCurrentUserAsync()
    {
        var result = await _httpHelper.GetAsync<AppUserDto>($"{Urls.ServerUrl}/api/v1/auth/me");
        return result;
    }

    public async Task<ResponseResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var result = await _httpHelper.PostAsync<ResponseResult<AuthResponse>>($"{Urls.ServerUrl}/api/v1/auth/refresh", request);

        return result;
    }

    public async Task<bool> SendEmailVerificationCodeAsync(SendEmailCodeRequest request)
    {
        var result = await _httpHelper.PostAsync($"{Urls.ServerUrl}/api/v1/auth/sendEmailCode", request);

        return result;
    }

    public async Task<ResponseResult<CodeSignInResponse>> SignInByCodeAsync(CodeSignInRequest request)
    {
        var result = await _httpHelper.PostAsync<ResponseResult<CodeSignInResponse>>($"{Urls.ServerUrl}/api/v1/auth/signInByCode", request);

        return result;
    }

    public async Task<ResponseResult<AuthResponse>> SignInByPasswordAsync(PasswordSignInRequest request)
    {
        var result = await _httpHelper.PostAsync<ResponseResult<AuthResponse>>($"{Urls.ServerUrl}/api/v1/auth/signInByPassword", request);

        return result;
    }

    public async Task<ResponseResult<string>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var result = await _httpHelper.PostAsync<ResponseResult<string>>($"{Urls.ServerUrl}/api/v1/auth/resetPassword", request);

        return result;
    }

    public async Task<ResponseResult<string>> UpdatePasswordAsync(UpdatePasswordRequest request)
    {
        var result = await _httpHelper.PostAsync<ResponseResult<string>>($"{Urls.ServerUrl}/api/v1/auth/updatePassword", request);

        return result;
    }
}
