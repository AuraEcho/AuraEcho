namespace AuraEcho.Core.Models.Api.Auth;

public class UpdateProfileRequest
{
    public string? UserName { get; set; }
    public Guid? AvatarFileId { get; set; }
}
