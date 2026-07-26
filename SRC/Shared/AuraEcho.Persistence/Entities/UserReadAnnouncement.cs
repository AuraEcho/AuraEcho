namespace AuraEcho.Persistence.Entities;

/// <summary>
/// 用户公告已读记录
/// </summary>
public class UserReadAnnouncement
{
    public Guid Id { get; set; }

    /// <summary>
    /// 用户 Id
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 公告 Id
    /// </summary>
    public Guid AnnouncementId { get; set; }

    /// <summary>
    /// 用户已读时该公告的更新时间
    /// </summary>
    public DateTime ReadVersion { get; set; }

    /// <summary>
    /// 已读时间（UTC）
    /// </summary>
    public DateTime ReadAt { get; set; }
}
