using AuraEcho.Core.Tools;
using AuraEcho.PluginContracts.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace AuraEcho.Core.Services;

public class WebImageLoader : IWebImageLoader
{
    private static readonly MemoryCache _memoryCache;
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// 图片 Url 对应的下载任务，用于优化同时加载相同图片的场景
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<Task<byte[]>>> ByteLoadingTasks = new();

    static WebImageLoader()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        if (!Directory.Exists(ApplicationPaths.ImageCache))
            Directory.CreateDirectory(ApplicationPaths.ImageCache);
    }

    /// <summary>
    /// 获取图片（支持解码尺寸优化内存）
    /// </summary>
    public async Task<BitmapImage> GetImageAsync(string url, int decodeWidth = 0, int decodeHeight = 0)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        // 内存缓存(图片+尺寸 BitmapImage)
        string memCacheKey = $"{url}_{decodeWidth}_{decodeHeight}";

        if (_memoryCache.Get(memCacheKey) is BitmapImage memImage)
        {
            return memImage;
        }
        
        // 获取或添加(如果不存在)下载任务
        var lazyByteTask = ByteLoadingTasks.GetOrAdd(url,
            key => new Lazy<Task<byte[]>>(() => GetImageBytesInternalAsync(key)));

        byte[] imageBytes;
        try
        {
            imageBytes = await lazyByteTask.Value.ConfigureAwait(false);
        }
        catch
        {
            return null;
        }

        // 构建 BitmapImage 对象
        var bitmap = CreateBitmapImageFromBytes(imageBytes, decodeWidth, decodeHeight);

        if (bitmap != null)
        {
            _memoryCache.Set(memCacheKey, bitmap, TimeSpan.FromMinutes(10));
        }

        return bitmap;
    }

    /// <summary>
    /// 磁盘读取与网络下载
    /// </summary>
    private static async Task<byte[]> GetImageBytesInternalAsync(string url)
    {
        try
        {
            string fileName = GetMd5Hash(url);
            string filePath = Path.Combine(ApplicationPaths.ImageCache, fileName);

            // 磁盘缓存
            if (File.Exists(filePath))
            {
                try
                {
                    return await File.ReadAllBytesAsync(filePath);
                }
                catch { /* 读取失败则降级为网络下载 */ }
            }

            // 网络请求
            byte[] bytes = await HttpClient.GetByteArrayAsync(url);

            // 缓存到磁盘
            _ = File.WriteAllBytesAsync(filePath, bytes);

            return bytes;
        }
        catch(Exception ex)
        {
            Debug.WriteLine("图片加载失败");
            return null;
        }
        finally
        {
            // 读取完成，移除当前任务
            ByteLoadingTasks.TryRemove(url, out _);
        }
    }

    /// <summary>
    /// 从图片数据解码对应大小的 BitmapImage
    /// </summary>
    private static BitmapImage CreateBitmapImageFromBytes(byte[] bytes, int decodeWidth, int decodeHeight)
    {
        if (bytes == null || bytes.Length == 0) return null;
        try
        {
            var bitmapImage = new BitmapImage();
            using (var stream = new MemoryStream(bytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                if (decodeWidth > 0)
                    bitmapImage.DecodePixelWidth = decodeWidth;

                if (decodeHeight > 0)
                    bitmapImage.DecodePixelHeight = decodeHeight;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }
            bitmapImage.Freeze();
            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

    private static string GetMd5Hash(string input)
    {
        byte[] data = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (byte b in data)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
