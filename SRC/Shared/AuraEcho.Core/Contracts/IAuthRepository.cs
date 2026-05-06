using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Models.Api.Auth;

namespace AuraEcho.Core.Contracts;

public interface IAuthRepository
{
    Task<ResponseResult<CodeSignInResponse>> SignInByCodeAsync(CodeSignInRequest request);
    Task<ResponseResult<AuthResponse>> SignInByPasswordAsync(PasswordSignInRequest request);
    Task<ResponseResult<string>> SendEmailVerificationCodeAsync(SendEmailCodeRequest request);
    Task<AppUserDto> GetCurrentUserAsync();
    Task<ResponseResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ResponseResult<string>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ResponseResult<string>> UpdatePasswordAsync(UpdatePasswordRequest request);
    Task<ResponseResult<string>> UpdateProfileAsync(UpdateProfileRequest request);
}
