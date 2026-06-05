using AuraEcho.Core.Contracts;
using AuraEcho.Core.Data;
using AuraEcho.Core.Data.Entities;
using AuraEcho.Core.Enums;
using AuraEcho.Core.Extensions;
using AuraEcho.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AuraEcho.Core.Repositories;

public class LocalPluginRepository : ILocalPluginRepository
{
    private readonly AuraEchoDbContext _dbContext;
    public LocalPluginRepository(AuraEchoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddLocalPluginAsync(InstalledPluginModel newPlugin)
    {
        await _dbContext.InstalledPlugin.AddAsync(newPlugin.ToLocalPlugin());
        await _dbContext.SaveChangesAsync();
    }

    public async Task<UserPluginModel> AddUserPluginAsync(Guid userId, Guid localPluginId)
    {
        var newUserPlugin = new UserPlugin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LocalPluginId = localPluginId,
        };
        await _dbContext.UserPlugin.AddAsync(newUserPlugin);
        await _dbContext.SaveChangesAsync();

        UserPlugin userPlugin =
            await _dbContext.UserPlugin
                            .Include(up => up.LocalPlugin)
                            .SingleAsync(up => up.Id == newUserPlugin.Id);

        return userPlugin.ToUserPluginModel();
    }

    public async Task<List<InstalledPluginModel>> GetLocalPluginsAsync()
    {
        var plugins = await _dbContext.InstalledPlugin.ToListAsync();
        return plugins.Select(p => p.ToLocalPluginModel()).ToList();
    }

    public async Task<UserPluginModel> GetUserPluginAsync(Guid userPluginId)
    {
        UserPlugin userPlugin =
            await _dbContext.UserPlugin
                            .Include(up => up.LocalPlugin)
                            .SingleAsync(up => up.Id == userPluginId);

        return userPlugin.ToUserPluginModel();
    }

    public async Task<List<UserPluginModel>> GetUserPluginsAsync(Guid userId)
    {
        List<UserPlugin> userPlugins =
            await _dbContext.UserPlugin
                            .Include(up => up.LocalPlugin)
                            .Where(up => up.UserId == userId)
                            .ToListAsync();

        return userPlugins.Select(up => up.ToUserPluginModel()).ToList();
    }

    public async Task RemoveLocalPluginAsync(Guid localPluginId)
    {
        InstalledPlugin? plugin = await _dbContext.InstalledPlugin.FindAsync(localPluginId);
        if (plugin is null) return;

        _dbContext.InstalledPlugin.Remove(plugin);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveUserPluginAsync(Guid userId, Guid localPluginId)
    {
        UserPlugin? userPlugin = await _dbContext.UserPlugin.FirstOrDefaultAsync(up => up.UserId == userId && up.LocalPluginId == localPluginId);
        if (userPlugin is null) return;

        _dbContext.UserPlugin.Remove(userPlugin);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveUserPluginAsync(Guid userPluginId)
    {
        UserPlugin? userPlugin = await _dbContext.UserPlugin.FindAsync(userPluginId);
        if (userPlugin is null) return;

        _dbContext.UserPlugin.Remove(userPlugin);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateLocalPluginAsync(InstalledPluginModel plugin)
    {
        if (plugin is null) throw new ArgumentNullException(nameof(plugin));

        var localPlugin = await _dbContext.InstalledPlugin.FirstOrDefaultAsync(lp => lp.PluginId == plugin.Id);

        if (localPlugin is null) return;

        localPlugin.PluginId = plugin.Id;
        localPlugin.PluginType = plugin.PluginType;
        localPlugin.InstallPath = plugin.InstallPath;
        localPlugin.InstaledAt = plugin.InstaledAt;
        localPlugin.Version = plugin.Version;
        localPlugin.IsSetup = plugin.IsSetup;

        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateUserPluginStatusAsync(Guid userId, Guid localPluginId, PluginPlanStatus newStatus)
    {
        UserPlugin? userPlugin = await _dbContext.UserPlugin.FirstOrDefaultAsync(up => up.UserId == userId && up.LocalPluginId == localPluginId);
        if (userPlugin is null) return;

        userPlugin.Status = newStatus;
        await _dbContext.SaveChangesAsync();
    }
}
