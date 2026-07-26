using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using AuraEcho.Models;

namespace AuraEcho.Interfaces;

/// <summary>
/// 公告服务
/// </summary>
public interface IAnnouncementService : INotifyPropertyChanged
{
    /// <summary>
    /// 当前生效的公告列表，按更新时间倒序。
    /// </summary>
    ReadOnlyObservableCollection<AnnouncementEntry> Announcements { get; }

    /// <summary>
    /// 是否存在未读公告。
    /// </summary>
    bool HasUnread { get; }

    /// <summary>
    /// 重新拉取当前生效的公告，并刷新未读状态。
    /// </summary>
    Task RefreshAsync();

    /// <summary>
    /// 将指定公告标记为已读
    /// </summary>
    Task MarkReadAsync(AnnouncementEntry entry);
}
