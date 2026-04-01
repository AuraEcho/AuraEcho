namespace AuraEcho.Core.Models.Api;

public class CodeSignInResponse
{
    public bool IsNewUser { get; set; }
    public AuthResponse Data { get; set; }
}
