using AuraEcho.Cloud.V1.Models.AppPackage;
using AuraEcho.Cloud.V1.Models.License;
using AuraEcho.Cloud.V1.Models.Plugin;
using AuraEcho.Cloud.V1.Models.Sku;
using AuraEcho.Core.Models;
using AuraEcho.Domain;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.Core.Extensions;

public static class ApiModelMappingExtensions
{
    public static RemotePlugin ToRemotePlugin(this ListPluginItem dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Summary = dto.Summary,
        Description = dto.Description,
        Author = dto.Author,
        IconFileUrl = dto.IconFileUrl,
        IconFileId = dto.IconFileId,
        BannerFileUrl = dto.BannerFileUrl,
        BannerFileId = dto.BannerFileId,
        IsAcquired = dto.IsAcquired,
        CreateTime = dto.CreateTime,
        UserCount = dto.UserCount
    };

    public static RemotePlugin ToRemotePlugin(this GetPluginByIdResult dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Summary = dto.Summary,
        Description = dto.Description,
        Author = dto.Author,
        IconFileUrl = dto.IconFileUrl,
        IconFileId = dto.IconFileId,
        BannerFileUrl = dto.BannerFileUrl,
        BannerFileId = dto.BannerFileId,
        IsAcquired = dto.IsAcquired,
        CreateTime = dto.CreateTime,
        UserCount = dto.UserCount
    };

    public static PluginPackage ToPluginPackage(this GetPluginLatestVersionResponse dto) => new()
    {
        Id = dto.Id,
        PluginId = dto.PluginId,
        Version = dto.Version,
        ReleaseNotes = dto.ReleaseNotes,
        FileId = dto.FileId,
        FileUrl = dto.FileUrl,
        FileName = dto.FileName,
        Size = dto.Size,
        CreateTime = dto.CreateTime
    };

    public static PluginScreenshot ToPluginScreenshot(this PluginScreenshotResponseItem dto) => new()
    {
        Id = dto.Id,
        PluginId = dto.PluginId,
        FileId = dto.FileId,
        FileUrl = dto.FileUrl,
        Order = dto.Order
    };

    public static Sku ToSku(this SkuInfo dto) => new()
    {
        Id = dto.Id!.Value,
        ResourceId = dto.ResourceId,
        ResourceType = dto.ResourceType,
        LicenseTierId = dto.LicenseTierId,
        TierLevel = dto.TierLevel,
        TierName = dto.TierName,
        DurationMonths = dto.DurationMonths,
        SalePrice = dto.SalePrice,
        OriginalPrice = dto.OriginalPrice,
        IsActive = dto.IsActive,
        Ordinal = dto.Ordinal
    };

    public static AppVersionInfo ToAppVersionInfo(this GetLatestVersionResponse dto) => new()
    {
        Version = dto.Version,
        FullFileId = dto.FullFileId,
        FullFileUrl = dto.FullFileUrl,
        FullFileName = dto.FullFileName,
        FullFileSize = dto.FullFileSize,
        UpdateFileId = dto.UpdateFileId,
        UpdateFileUrl = dto.UpdateFileUrl,
        UpdateFileName = dto.UpdateFileName,
        UpdateFileSize = dto.UpdateFileSize,
        ReleaseDate = dto.ReleaseDate
    };

    public static ResourceLicense ToResourceLicense(this LicenseResponseItem dto) => new()
    {
        IsValid = dto.IsValid,
        ResourceId = dto.ResourceId,
        TierLevel = dto.TierLevel,
        TierName = dto.TierName,
        TierDescription = dto.TierDescription,
        ExpiredAt = dto.ExpiredAt
    };
}
