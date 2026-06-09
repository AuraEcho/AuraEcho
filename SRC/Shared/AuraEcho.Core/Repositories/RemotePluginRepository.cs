using AuraEcho.Api.Models.V1.Common;
using AuraEcho.Api.Models.V1.Plugin;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Repositories;

public class RemotePluginRepository : IRemotePluginRepository
{
    private HttpHelper _httpHelper;
    public RemotePluginRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }

    public async Task<PluginPackage> GetLatestAsync(Guid pluginId)
    {
        var result = await _httpHelper.GetAsync<GetPluginLatestVersionResponse>(Urls.GetLatestPluginVersion(pluginId));
        if (result is null) return null;

        return new PluginPackage
        {
            PluginId = result.PluginId,
            FileName = result.FileName,
            FileId = result.FileId,
            ReleaseNotes = result.ReleaseNotes,
            CreateTime = result.CreateTime,
            Id = result.Id,
            FileUrl = result.FileUrl,
            Size = result.Size,
            Version = result.Version
        };
    }
    public async Task<List<RemotePlugin>> GetPluginsAsync()
    {
        var result = await _httpHelper.GetAsync<ResponseResult<List<ListPluginItem>>>(Urls.GetPlugins());
        if (result is null) return null;

        List<RemotePlugin> plugins =
            result.Data
                  .Select(p => new RemotePlugin
                  {
                      Author = p.Author,
                      Name = p.Name,
                      CreateTime = p.CreateTime,
                      Id = p.Id,
                      IconFileUrl = p.IconFileUrl,
                      BannerFileUrl = p.BannerFileUrl,
                      Description = p.Description,
                      Summary = p.Summary,
                      BannerFileId = p.BannerFileId,
                      IconFileId = p.IconFileId,
                      IsAcquired = p.IsAcquired,
                      UserCount = p.UserCount
                  })
                  .ToList();

        return plugins;
    }
    public async Task<RemotePlugin> GetPluginByIdAsync(Guid pluginId)
    {
        var result = await _httpHelper.GetAsync<GetPluginByIdResult>(Urls.GetPluginById(pluginId));
        if (result is null) return null;

        var plugin = new RemotePlugin
        {
            Author = result.Author,
            Name = result.Name,
            CreateTime = result.CreateTime,
            Id = result.Id,
            IconFileUrl = result.IconFileUrl,
            BannerFileUrl = result.BannerFileUrl,
            Description = result.Description,
            Summary = result.Summary,
            BannerFileId = result.BannerFileId,
            IconFileId = result.IconFileId,
            IsAcquired = result.IsAcquired,
            UserCount = result.UserCount
        };

        return plugin;
    }
    public async Task<List<RemotePlugin>> GetAllPluginsAsync()
    {
        var result = await _httpHelper.GetAsync<ListPluginsResponse>(Urls.GetAllPlugins());
        if (result is null) return null;

        List<RemotePlugin> plugins =
            result.Plugins
                  .Select(p => new RemotePlugin
                  {
                      Author = p.Author,
                      Name = p.Name,
                      CreateTime = p.CreateTime,
                      Id = p.Id,
                      Description = p.Description,
                      Summary = p.Summary,
                      IconFileId = p.IconFileId,
                  })
                  .ToList();

        return plugins;
    }
    public async Task<bool> AcquireAsync(Guid pluginId)
    {
        var result = await _httpHelper.PostAsync<ResponseResult<string>>(Urls.AcquirePlugin(pluginId), null);
        return result is not null;
    }
    public async Task<List<PluginScreenshot>> GetScreenshotsAsync(Guid pluginId)
    {
        var result = await _httpHelper.GetAsync<GetPluginScreenshotsResponse>(Urls.GetPluginScreenshots(pluginId));
        if (result is null) return [];

        List<PluginScreenshot> screenshots =
            result.Screenshots
                  .Select(s => new PluginScreenshot
                  {
                      Id = s.Id,
                      FileId = s.FileId,
                      FileUrl = s.FileUrl,
                      PluginId = s.PluginId,
                      Order = s.Order
                  })
                  .ToList();

        return screenshots;
    }
}

