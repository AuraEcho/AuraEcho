using System.IO;
using System.Net.Http;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Repositories;

public class RemotePluginRepository : IRemotePluginRepository
{
    private HttpHelper _httpHelper;
    public RemotePluginRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }

    public async Task<Guid?> CreatePluginAsync(CreatePluginRequest req)
    {
        var resp = await _httpHelper.PostAsync<CreatePluginResponse>($"{Urls.ServerUrl}/api/v1/plugin/create", req);
        if (resp is null) return null;

        return resp.PluginId;
    }

    public async Task<Guid?> CreateVersionAsync(CreatePluginVersionRequest req)
    {
        var resp = await _httpHelper.PostAsync<CreatePluginVersionResponse>($"{Urls.ServerUrl}/api/v1/plugin/createVersion", req);
        if (resp is null) return null;

        return resp.PackageId;
    }

    public async Task<bool> DeleteAsync(Guid pluginId)
    {
        bool result = await _httpHelper.DeleteAsync($"{Urls.ServerUrl}/api/v1/plugin/delete/{pluginId}");
        return result;
    }

    public async Task<bool> DeleteVersionAsync(Guid versionId)
    {
        var result = await _httpHelper.DeleteAsync($"{Urls.ServerUrl}/api/v1/plugin/deleteVersion/{versionId}");
        return result;
    }

    public async Task<bool> DownloadLatestAsync(Guid pluginId, string build, string outputPath, IProgress<double> progress)
    {
        try
        {
            using var response = await _httpHelper.GetAsync($"{Urls.ServerUrl}/api/v1/plugin/download?pluginId={pluginId}&build={build}", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[80 * 1024];
            long totalRead = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                totalRead += read;

                if (totalBytes > 0)
                {
                    double percent = totalRead * 100.0 / totalBytes;
                    progress?.Report(percent);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PluginPackage> GetLatestAsync(Guid pluginId)
    {
        var result = await _httpHelper.GetAsync<GetPluginLatestVersionResponse>($"{Urls.ServerUrl}/api/v1/plugin/latest?pluginId={pluginId}");
        if (result is null) return null;

        return new PluginPackage
        {
            PluginId = result.PluginId,
            FileName = result.FileName,
            FileId = result.FileId,
            ReleaseNotes = result.ReleaseNotes,
            CreateTime = result.CreateTime,
            Id = result.Id,
            Size = result.Size,
            Version = result.Version
        };
    }

    public async Task<List<RemotePlugin>> GetPluginsAsync()
    {
        var result = await _httpHelper.GetAsync<ResponseResult<List<ListPluginItem>>>($"{Urls.ServerUrl}/api/v1/plugin/list");
        if (result is null) return null;

        List<RemotePlugin> plugins =
            result.Data
                  .Select(p => new RemotePlugin
                  {
                      Author = p.Author,
                      Name = p.Name,
                      CreateTime = p.CreateTime,
                      Id = p.Id,
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

    public async Task<List<RemotePlugin>> GetAllPluginsAsync()
    {
        var result = await _httpHelper.GetAsync<ListPluginsResponse>($"{Urls.ServerUrl}/api/v1/plugin/listAll");
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
                      Summary= p.Summary,
                      IconFileId = p.IconFileId,
                  })
                  .ToList();

        return plugins;
    }

    public async Task<List<PluginPackage>> GetVersionsAsync(Guid pluginId)
    {
        var result = await _httpHelper.GetAsync<GetPluginActivedVerionsResponse>($"{Urls.ServerUrl}/api/v1/plugin/versions?pluginId={pluginId}");
        if (result is null) return null;

        var pluginVersions =
            result.Versions
                  .Select(v => new PluginPackage
                  {
                      CreateTime = v.CreateTime,
                      Id = v.Id,
                      ReleaseNotes = v.ReleaseNotes,
                      Version = v.Version,
                      FileId = v.FileId,
                      FileName = v.FileName,
                      PluginId = v.PluginId,
                      Size = v.Size
                  })
                  .ToList();
        return pluginVersions;
    }

    public async Task<bool> AcquireAsync(Guid userId, Guid pluginId)
    {
        var result = await _httpHelper.PostAsync<ResponseResult<string>>($"{Urls.ServerUrl}/api/v1/plugin/{pluginId}/acquire", null);
        return result is not null;
    }
}

