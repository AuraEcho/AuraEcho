using AuraEcho.Persistence.Entities;

namespace AuraEcho.Persistence.Contracts;

/// <summary>
/// 用户公告已读记录仓储
/// </summary>
public interface IUserAnnouncementRepository
{
    /// <summary>
    /// 获取指定用户的全部已读记录。
    /// </summary>
    Task<List<UserReadAnnouncement>> GetReadRecordsAsync(Guid userId);

    /// <summary>
    /// 将单条公告标记为已读
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="announcementId">公告标识</param>
    /// <param name="updatedAt">该公告的 UpdatedAt，作为已读版本记录</param>
    Task MarkReadAsync(Guid userId, Guid announcementId, DateTime updatedAt);

    /// <summary>
    /// 将一组公告批量标记为已读
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="reads">公告标识与其 UpdatedAt 的集合</param>
    Task MarkReadAsync(Guid userId, IEnumerable<(Guid AnnouncementId, DateTime UpdatedAt)> reads);

    /// <summary>
    /// 清理指定用户已读表中、不在 <paramref name="activeAnnouncementIds"/> 内的记录。
    /// </summary>
    Task PruneAsync(Guid userId, IEnumerable<Guid> activeAnnouncementIds);
}
