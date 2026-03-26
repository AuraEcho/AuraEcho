using AuraEcho.Core.Contracts;
using AuraEcho.Core.Data;
using AuraEcho.Core.Data.Entities;
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

    public async Task AddLocalPluginAsync(LocalPluginModel newPlugin)
    {
        await _dbContext.LocalPlugins.AddAsync(newPlugin.ToLocalPlugin());
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
        await _dbContext.UserPlugins.AddAsync(newUserPlugin);
        await _dbContext.SaveChangesAsync();

        UserPlugin userPlugin = 
            await _dbContext.UserPlugins
                            .Include(up => up.LocalPlugin)
                            .SingleAsync(up => up.Id == newUserPlugin.Id);

        return userPlugin.ToUserPluginModel();
    }

    public async Task<List<LocalPluginModel>> GetLocalPluginsAsync()
    {
        var plugins = await _dbContext.LocalPlugins.ToListAsync();
        return plugins.Select(p => p.ToLocalPluginModel()).ToList();
    }

    public async Task<UserPluginModel> GetUserPluginAsync(Guid userPluginId)
    {
        UserPlugin userPlugin =
            await _dbContext.UserPlugins
                            .Include(up => up.LocalPlugin)
                            .SingleAsync(up => up.Id == userPluginId);

        return userPlugin.ToUserPluginModel();
    }

    public async Task<List<UserPluginModel>> GetUserPluginsAsync(Guid userId)
    {
        List<UserPlugin> userPlugins =
            await _dbContext.UserPlugins
                            .Include(up => up.LocalPlugin)
                            .Where(up => up.UserId == userId)
                            .ToListAsync();

        return userPlugins.Select(up => up.ToUserPluginModel()).ToList();
    }

    public async Task RemoveLocalPluginAsync(Guid localPluginId)
    {
        LocalPlugin? plugin = await _dbContext.LocalPlugins.FindAsync(localPluginId);
        if (plugin is null) return;

        _dbContext.LocalPlugins.Remove(plugin);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveUserPluginAsync(Guid userId, Guid localPluginId)
    {
        UserPlugin? userPlugin = await _dbContext.UserPlugins.FirstOrDefaultAsync(up => up.UserId == userId && up.LocalPluginId == localPluginId);
        if (userPlugin is null) return;

        _dbContext.UserPlugins.Remove(userPlugin);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveUserPluginAsync(Guid userPluginId)
    {
        UserPlugin? userPlugin = await _dbContext.UserPlugins.FindAsync(userPluginId);
        if (userPlugin is null) return;

        _dbContext.UserPlugins.Remove(userPlugin);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateLocalPluginAsync(LocalPluginModel plugin)
    {
        var localPlugin = await _dbContext.LocalPlugins.FirstOrDefaultAsync(lp => lp.Id == plugin.Id);

        localPlugin.Manifest = plugin.Manifest;
        localPlugin.PluginFolder = plugin.PluginFolder;
        localPlugin.IsSetup = plugin.IsSetup;
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateUserPluginStatusAsync(Guid userId, Guid localPluginId, PluginPlanStatus newStatus)
    {
        UserPlugin? userPlugin = await _dbContext.UserPlugins.FirstOrDefaultAsync(up => up.UserId == userId && up.LocalPluginId == localPluginId);
        if (userPlugin is null) return;

        userPlugin.Status = newStatus;
        await _dbContext.SaveChangesAsync();
    }
}
