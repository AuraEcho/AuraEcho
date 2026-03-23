using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Models.Api.Auth;

namespace AuraEcho.Core.Contracts;

public interface IAuthRepository
{
    Task<ResponseResult<CodeSignInResponse>> SignInByCodeAsync(CodeSignInRequest request);
    Task<ResponseResult<AuthResponse>> SignInByPasswordAsync(PasswordSignInRequest request);
    Task<bool> SendEmailVerificationCodeAsync(SendEmailCodeRequest request);
    Task<MeResponse> GetCurrentUserAsync();
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ResponseResult<string>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ResponseResult<string>> UpdatePasswordAsync(UpdatePasswordRequest request);
}
