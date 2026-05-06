namespace AuraEcho.Core.Models.Api;

public class AppUserDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; }
    public Guid? AvatarFileId { get; set; }
}
