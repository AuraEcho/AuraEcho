namespace AuraEcho.Core.Models.Api;

public class AuthResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public AppUserDto User { get; set; }
}
