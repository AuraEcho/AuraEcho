using Prism.Mvvm;

namespace AuraEcho.Core.Models;

public class UserProfile : BindableBase
{
    public Guid Id { get; init; }
    public string UserName
    {
        get;
        set => SetProperty(ref field, value);
    }
    

    public string Email { get; init; }
    public Guid? AvatarFileId
    {
        get;
        set => SetProperty(ref field, value);
    }
}

