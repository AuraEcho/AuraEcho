using AuraEcho.Core.Models.Api;

namespace AuraEcho.Core.Contracts;

public interface IAuthRepository
{
    Task<ResponseResult<CodeSignInResponse>> SignInByCodeAsync(CodeSignInRequest request);
    Task<ResponseResult<AuthResponse>> SignInByPasswordAsync(PasswordSignInRequest request);
    Task<bool> SendEmailVerificationCodeAsync(string targetEmail);
    Task<MeResponse> GetCurrentUserAsync();
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
}
