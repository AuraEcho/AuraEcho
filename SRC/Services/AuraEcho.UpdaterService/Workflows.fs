module AuraEcho.UpdaterService.Workflows

open System
open System.IO
open AuraEcho.Cloud.V1
open AuraEcho.Core.Contracts
open AuraEcho.Core.Extensions
open AuraEcho.Persistence.Contracts
open AuraEcho.UpdaterService.Utils
open Microsoft.Data.Sqlite
open Microsoft.Extensions.Logging
open System.IO.Pipes

let private APP_PIPE_NAME = "AURAECHO_APP_PIPE"

// --- 客户端更新 ---
let updateAppAsync (logger: ILogger) (apiClient: ApiClient) cachePath = task {
    logger.LogInformation("开始检测客户端版本信息...")

    try
        let currentVersion = SystemInfo.getInstalledVersion()
        let! latestResponse = apiClient.AppPackage.GetLatestAsync()
        let latestInfo = Option.ofObj latestResponse |> Option.map (fun r -> r.ToAppVersionInfo())

        let latestVersion =
            match latestInfo with
            | None -> Version ("1.0.0")
            | Some info -> Version(info.Version)

        if latestVersion > currentVersion then
            match latestInfo with
            | None -> logger.LogWarning("无法获取最新版本信息")
            | Some info ->
                logger.LogInformation("发现新版本 {LatestVersion}，正在下载...", latestVersion)
                let targetPath = Path.Combine(cachePath, info.UpdateFileName)
                let! downloaded = apiClient.File.DownloadFileAsync(info.UpdateFileUrl, targetPath, null)
                if downloaded then
                    logger.LogInformation("客户端下载完成，准备安装...")
                    match! ProcessHelper.runInstallerAsync targetPath with
                    | Ok _ ->
                        logger.LogInformation("客户端更新安装完成")
                        if File.Exists(targetPath) then File.Delete(targetPath)
                    | Error msg ->
                        logger.LogError("安装程序执行失败: {Message}", msg)
                else
                    logger.LogWarning("客户端安装包下载失败")
        else
            logger.LogDebug("客户端已是最新版本")
    with ex ->
        logger.LogError(ex, "客户端更新流程发生异常")
}

// 插件更新完成，通知 app
let notifyAppPluginUpdateAsync (logger: ILogger) pluginId newVersion = task {
    logger.LogInformation("正在通知客户端插件更新完成...")
    try
        use client = new NamedPipeClientStream(".", APP_PIPE_NAME, PipeDirection.Out)
        do! client.ConnectAsync(200)
        use writer = new StreamWriter(client)
        do! writer.WriteLineAsync($"PluginNewVersion:{pluginId}:{newVersion}")
        do! writer.FlushAsync()
        logger.LogInformation("已完成通知")
    with 
    | :? TimeoutException -> logger.LogWarning("未完成通知：连接客户端超时")
    | ex -> logger.LogError(ex, "未完成通知：未知异常")
}

// --- 插件更新 ---
let updatePluginsAsync (logger: ILogger) (localRepo: ILocalPluginRepository) (apiClient: ApiClient) (installer: IPluginInstallService) cachePath = task {
    logger.LogInformation("开始检测插件版本...")

    try
        let! installedPlugins = localRepo.GetLocalPluginsAsync()
        SqliteConnection.ClearAllPools()

        for plugin in installedPlugins do
            let! remoteResponse = apiClient.Plugin.GetLatestAsync(plugin.PluginId)
            let remotePackage = Option.ofObj remoteResponse |> Option.map (fun r -> r.ToPluginPackage())
            let remoteVer =
                match remotePackage with
                | None -> Version("0.0.0")
                | Some pkg -> Version(pkg.Version)
            let localVer = Version(plugin.Version)

            match remoteVer > localVer with
            | true ->
                match remotePackage with
                | None -> logger.LogWarning("无法获取插件 {PluginId} 的最新版本信息", plugin.PluginId)
                | Some pkg ->
                    logger.LogInformation("发现插件 {PluginId} 的新版本 {RemoteVersion}", plugin.PluginId, remoteVer)
                    let targetPath = Path.Combine(cachePath, pkg.FileName)

                    let! downloaded = apiClient.File.DownloadFileAsync(pkg.FileUrl, targetPath, null)
                    if downloaded then
                        try
                            let! _ = installer.InstallAsync(targetPath)
                            File.Delete(targetPath)
                            logger.LogInformation("插件 {PluginId} 更新成功", plugin.PluginId)
                            do! notifyAppPluginUpdateAsync logger plugin.PluginId pkg.Version
                        with ex ->
                            logger.LogError(ex, "安装插件 {PluginId} 时失败", plugin.PluginId)
                    else
                        logger.LogWarning("插件 {PluginId} 下载失败", plugin.PluginId)
            | false -> logger.LogDebug("插件 {PluginId} 已是最新版本", plugin.PluginId)

    with ex ->
        logger.LogError(ex, "插件更新流程发生异常")
}