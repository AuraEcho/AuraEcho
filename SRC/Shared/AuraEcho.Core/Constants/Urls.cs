using AuraEcho.Api.Models.V1.Order;

namespace AuraEcho.Core.Constants;

public static class Urls
{
    public static string ServerUrl => "http://localhost:5177";

    #region AppPackage
    public static string CreatePackage() => $"{ServerUrl}/v1/package/create";
    public static string DeletePackage(Guid packageId) => $"{ServerUrl}/v1/package/delete/{packageId}";
    public static string DownloadLatestPackage(bool isFull) => $"{ServerUrl}/v1/package/download?isFull={isFull}";
    public static string GetLatestPackageVersion() => $"{ServerUrl}/v1/package/latest";
    public static string GetUploadedPackages() => $"{ServerUrl}/v1/package/listAll";
    #endregion

    #region Auth
    public static string GetCurrentUser() => $"{ServerUrl}/v1/auth/me";
    public static string RefreshToken() => $"{ServerUrl}/v1/auth/refresh";
    public static string SendEmailVerificationCode() => $"{ServerUrl}/v1/auth/sendEmailCode";
    public static string SignInByCode() => $"{ServerUrl}/v1/auth/signInByCode";
    public static string SignInByPassword() => $"{ServerUrl}/v1/auth/signInByPassword";
    public static string ResetPassword() => $"{ServerUrl}/v1/auth/resetPassword";
    public static string UpdatePassword() => $"{ServerUrl}/v1/auth/updatePassword";
    public static string UpdateProfile() => $"{ServerUrl}/v1/auth/me";
    #endregion

    #region File

    public static string GetStsToken() => $"{ServerUrl}/v1/file/sts";
    public static string FileCheck(string contentHash, string fileName) => $"{Urls.ServerUrl}/v1/file/check?contentHash={contentHash}&fileName={fileName}";
    public static string FileCallback() => $"{ServerUrl}/v1/file-callback/oss";
    #endregion

    #region Plugin
    public static string CreatePlugin() => $"{ServerUrl}/v1/plugin/create";
    public static string CreatePluginVersion() => $"{ServerUrl}/v1/plugin/createVersion";
    public static string GetPlugins() => $"{ServerUrl}/v1/plugin/list";
    public static string GetPluginById(Guid pluginId) => $"{ServerUrl}/v1/plugin/{pluginId}";
    public static string GetAllPlugins() => $"{ServerUrl}/v1/plugin/listAll";
    public static string GetPluginVersions(Guid pluginId) => $"{ServerUrl}/v1/plugin/versions?pluginId={pluginId}";
    public static string GetLatestPluginVersion(Guid pluginId) => $"{ServerUrl}/v1/plugin/latest?pluginId={pluginId}";
    public static string DownloadPluginLatest(Guid pluginId, string build) => $"{ServerUrl}/v1/plugin/download?pluginId={pluginId}&build={build}";
    public static string DeletePlugin(Guid pluginId) => $"{ServerUrl}/v1/plugin/delete/{pluginId}";
    public static string DeletePluginVersion(Guid versionId) => $"{ServerUrl}/v1/plugin/deleteVersion/{versionId}";
    public static string AcquirePlugin(Guid pluginId) => $"{ServerUrl}/v1/plugin/{pluginId}/acquire";
    public static string GetPluginScreenshots(Guid pluginId) => $"{ServerUrl}/v1/plugin/{pluginId}/screenshots";
    #endregion

    #region Sku
    public static string GetResourceSkus(Guid resourceId) => $"{ServerUrl}/v1/sku/list?resourceId={resourceId}";
    public static string GetSkuById(Guid skuId) => $"{ServerUrl}/v1/sku/{skuId}";
    #endregion

    #region License
    public static string GetResourceLicense(Guid resourceId) => $"{ServerUrl}/v1/license/{resourceId}";
    public static string GetUserLicenses() => $"{ServerUrl}/v1/license/my-licenses";
    #endregion

    #region Order
    public static string CreateOrderAsync() => $"{ServerUrl}/v1/order/create";
    public static string GetOrderStatus(Guid orderId) => $"{ServerUrl}/v1/order/status/{orderId}";
    public static string GetOrderById(Guid orderId) => $"{ServerUrl}/v1/order/{orderId}";
    #endregion
}
