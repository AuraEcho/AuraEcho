using AuraEcho.Cloud.V1.Models.Announcement;
using Prism.Mvvm;

namespace AuraEcho.Models;

/// <summary>
/// 公告列表项
/// </summary>
public class AnnouncementEntry : BindableBase
{
    public AnnouncementEntry(AnnouncementItem item, bool isUnread)
    {
        Item = item;
        IsUnread = isUnread;
    }

    /// <summary>
    /// 公告数据
    /// </summary>
    public AnnouncementItem Item { get; }

    /// <summary>
    /// 是否未读。用户未登录时恒为 false。
    /// </summary>
    public bool IsUnread
    {
        get;
        set => SetProperty(ref field, value);
    }
}
