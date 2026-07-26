using AuraEcho.Persistence.Contracts;
using AuraEcho.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuraEcho.Persistence.Repositories;

public class UserAnnouncementRepository : IUserAnnouncementRepository
{
    private readonly HostDbContext _dbContext;

    public UserAnnouncementRepository(HostDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<UserReadAnnouncement>> GetReadRecordsAsync(Guid userId)
    {
        return await _dbContext.UserReadAnnouncement
                               .Where(r => r.UserId == userId)
                               .ToListAsync();
    }

    public async Task MarkReadAsync(Guid userId, Guid announcementId, DateTime updatedAt)
    {
        UserReadAnnouncement? record =
            await _dbContext.UserReadAnnouncement
                            .FirstOrDefaultAsync(r => r.UserId == userId
                                                   && r.AnnouncementId == announcementId);

        var now = DateTime.UtcNow;
        if (record is null)
        {
            await _dbContext.UserReadAnnouncement.AddAsync(new UserReadAnnouncement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AnnouncementId = announcementId,
                ReadVersion = updatedAt,
                ReadAt = now
            });
        }
        else
        {
            record.ReadVersion = updatedAt;
            record.ReadAt = now;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task MarkReadAsync(Guid userId, IEnumerable<(Guid AnnouncementId, DateTime UpdatedAt)> reads)
    {
        var readList = reads.ToList();
        if (readList.Count == 0) return;

        var ids = readList.Select(r => r.AnnouncementId).ToList();
        var existing = await _dbContext.UserReadAnnouncement
                                       .Where(r => r.UserId == userId && ids.Contains(r.AnnouncementId))
                                       .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var (announcementId, updatedAt) in readList)
        {
            var record = existing.FirstOrDefault(r => r.AnnouncementId == announcementId);
            if (record is null)
            {
                await _dbContext.UserReadAnnouncement.AddAsync(new UserReadAnnouncement
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AnnouncementId = announcementId,
                    ReadVersion = updatedAt,
                    ReadAt = now
                });
            }
            else
            {
                record.ReadVersion = updatedAt;
                record.ReadAt = now;
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task PruneAsync(Guid userId, IEnumerable<Guid> activeAnnouncementIds)
    {
        var activeSet = activeAnnouncementIds.ToHashSet();
        var stale = await _dbContext.UserReadAnnouncement
                                    .Where(r => r.UserId == userId)
                                    .ToListAsync();

        var toRemove = stale.Where(r => !activeSet.Contains(r.AnnouncementId)).ToList();
        if (toRemove.Count == 0) return;

        _dbContext.UserReadAnnouncement.RemoveRange(toRemove);
        await _dbContext.SaveChangesAsync();
    }
}
