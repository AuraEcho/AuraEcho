using AuraEcho.Api.Models.V1.Order;

namespace AuraEcho.Core.Constants;

public static class Urls
{
    private static string ServerUrl => "http://localhost:5177";

    #region AppPackage
    public static string CreatePackage() => $"{ServerUrl}/api/v1/package/create";
    public static string DeletePackage(Guid packageId) => $"{ServerUrl}/api/v1/package/delete/{packageId}";
    public static string DownloadLatestPackage(bool isFull) => $"{ServerUrl}/api/v1/package/download?isFull={isFull}";
    public static string GetLatestPackageVersion() => $"{ServerUrl}/api/v1/package/latest";
    public static string GetUploadedPackages() => $"{ServerUrl}/api/v1/package/listAll";
    #endregion

    #region Auth
    public static string GetCurrentUser() => $"{ServerUrl}/api/v1/auth/me";
    public static string RefreshToken() => $"{ServerUrl}/api/v1/auth/refresh";
    public static string SendEmailVerificationCode() => $"{ServerUrl}/api/v1/auth/sendEmailCode";
    public static string SignInByCode() => $"{ServerUrl}/api/v1/auth/signInByCode";
    public static string SignInByPassword() => $"{ServerUrl}/api/v1/auth/signInByPassword";
    public static string ResetPassword() => $"{ServerUrl}/api/v1/auth/resetPassword";
    public static string UpdatePassword() => $"{ServerUrl}/api/v1/auth/updatePassword";
    public static string UpdateProfile() => $"{ServerUrl}/api/v1/auth/me";
    #endregion

    #region File

    public static string GetStsToken() => $"{ServerUrl}/api/v1/file/sts";
    public static string FileCheck(string contentHash, string fileName) => $"{Urls.ServerUrl}/api/v1/file/check?contentHash={contentHash}&fileName={fileName}";
    public static string FileCallback() => $"{ServerUrl}/api/v1/file-callback/oss";

    public static string DownloadFile(Guid fileId) => $"{ServerUrl}/api/v1/file/download?fileId={fileId}";
    public static string GetFileById(Guid fileId) => $"{ServerUrl}/api/v1/file/{fileId}";
    public static string GetUploadedFiles() => $"{ServerUrl}/api/v1/file/UploadFileList";
    public static string UploadFile() => $"{ServerUrl}/api/v1/file/upload";
    public static string UploadFileInit() => $"{ServerUrl}/api/v1/file/uploadinit";
    public static string GetUploadedChunks(Guid uploadId) => $"{ServerUrl}/api/v1/file/uploadedChunks?uploadId={uploadId}";
    public static string UploadChunk() => $"{ServerUrl}/api/v1/file/uploadchunk";
    public static string UploadMerge() => $"{ServerUrl}/api/v1/file/uploadMerge";
    #endregion

    #region Plugin
    public static string CreatePlugin() => $"{ServerUrl}/api/v1/plugin/create";
    public static string CreatePluginVersion() => $"{ServerUrl}/api/v1/plugin/createVersion";
    public static string GetPlugins() => $"{ServerUrl}/api/v1/plugin/list";
    public static string GetPluginById(Guid pluginId) => $"{ServerUrl}/api/v1/plugin/{pluginId}";
    public static string GetAllPlugins() => $"{ServerUrl}/api/v1/plugin/listAll";
    public static string GetPluginVersions(Guid pluginId) => $"{ServerUrl}/api/v1/plugin/versions?pluginId={pluginId}";
    public static string GetLatestPluginVersion(Guid pluginId) => $"{ServerUrl}/api/v1/plugin/latest?pluginId={pluginId}";
    public static string DownloadPluginLatest(Guid pluginId, string build) => $"{ServerUrl}/api/v1/plugin/download?pluginId={pluginId}&build={build}";
    public static string DeletePlugin(Guid pluginId) => $"{ServerUrl}/api/v1/plugin/delete/{pluginId}";
    public static string DeletePluginVersion(Guid versionId) => $"{ServerUrl}/api/v1/plugin/deleteVersion/{versionId}";
    public static string AcquirePlugin(Guid pluginId) => $"{ServerUrl}/api/v1/plugin/{pluginId}/acquire";
    public static string GetPluginScreenshots(Guid pluginId) => $"{ServerUrl}/api/v1/plugin/{pluginId}/screenshots";
    #endregion

    #region Sku
    public static string GetResourceSkus(Guid resourceId) => $"{ServerUrl}/api/v1/sku/list?resourceId={resourceId}";
    #endregion

    #region License
    public static string GetResourceLicense(Guid resourceId) => $"{ServerUrl}/api/v1/license/{resourceId}";
    public static string GetUserLicenses() => $"{ServerUrl}/api/v1/license/my-licenses";
    #endregion

    #region Order
    public static string CreateOrderAsync() => $"{ServerUrl}/api/v1/order/create";
    public static string GetOrderPaymentCredential(Guid orderId, PaymentChannel channel) => $"{ServerUrl}/api/v1/order/{orderId}/payment-credential?channel={channel}";
    public static string GetOrderStatus(Guid orderId) => $"{ServerUrl}/api/v1/order/status/{orderId}";
    #endregion
}
