namespace AuraEcho.Core.Models.Api.Auth;

public class UpdatePasswordRequest
{
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
}
