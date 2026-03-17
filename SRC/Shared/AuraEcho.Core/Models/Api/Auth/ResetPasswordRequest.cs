namespace AuraEcho.Core.Models.Api.Auth;

public class ResetPasswordRequest
{
    public string Email { get; set; }

    public string EmailCode { get; set; }

    public string NewPassword { get; set; }
}
