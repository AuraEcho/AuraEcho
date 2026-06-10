module AuraEcho.UpdaterService.Workflows

open System
open System.IO
open Microsoft.Extensions.Logging
open AuraEcho.Core.Contracts
open AuraEcho.PluginContracts.Interfaces
open AuraEcho.UpdaterService.Utils
open Microsoft.Data.Sqlite
open System.IO.Pipes

let private APP_PIPE_NAME = "AURAECHO_APP_PIPE"

// --- 客户端更新 ---
let updateAppAsync (logger: IAppLogger) (repo: IAppPackageRepository) (storageRepository: IStorageRepository) cachePath = task {
    logger.Information("开始检测客户端版本信息...")
    
    try
        let currentVersion = SystemInfo.getInstalledVersion()
        let! latestInfo = repo.GetLatestAsync()
        
        let latestVersion = 
            match Option.ofObj latestInfo with
            | None -> Version ("1.0.0")
            | Some _ -> Version(latestInfo.Version)

        if latestVersion > currentVersion then
            logger.Information($"发现新版本 {latestVersion}，正在下载...")
            let targetPath = Path.Combine(cachePath, latestInfo.UpdateFileName)
            let! latestPackage = repo.GetLatestAsync() |> Async.AwaitTask
            let! downloaded = storageRepository.DownloadFileAsync(latestPackage.UpdateFileUrl, targetPath, null)
            if downloaded then
                logger.Information("客户端下载完成，准备安装...")
                match! ProcessHelper.runInstallerAsync targetPath with
                | Ok _ -> 
                    logger.Information("客户端更新安装完成")
                    if File.Exists(targetPath) then File.Delete(targetPath)
                | Error msg -> 
                    logger.Error($"安装程序执行失败: {msg}")
            else
                logger.Warning("客户端安装包下载失败")
        else
            logger.Debug("客户端已是最新版本")
    with ex ->
        logger.Error($"客户端更新流程发生异常{ex}")
}

// 插件更新完成，通知 app
let notifyAppPluginUpdateAsync (logger: IAppLogger) pluginId newVersion = task {
    logger.Information("正在通知客户端插件更新完成...")
    try
        use client = new NamedPipeClientStream(".", APP_PIPE_NAME, PipeDirection.Out)
        do! client.ConnectAsync(200)
        use writer = new StreamWriter(client)
        do! writer.WriteLineAsync($"PluginNewVersion:{pluginId}:{newVersion}")
        do! writer.FlushAsync()
        logger.Information("已完成通知")
    with ex -> 
        logger.Error($"通知客户端插件更新完成发生异常{ex}")
}

// --- 插件更新 ---
let updatePluginsAsync (logger: IAppLogger) (localRepo: ILocalPluginRepository) (remoteRepo: IRemotePluginRepository) (storageRepository: IStorageRepository) (installer: IPluginInstallService) cachePath = task {
    logger.Information("开始检测插件版本...")
    
    try
        let! installedPlugins = localRepo.GetLocalPluginsAsync()
        SqliteConnection.ClearAllPools()

        for plugin in installedPlugins do
            let! remotePackage = remoteRepo.GetLatestAsync(plugin.PluginId)
            let remoteVer = if isNull remotePackage then Version("0.0.0") else Version(remotePackage.Version)
            let localVer = Version(plugin.Version)

            match remoteVer > localVer with
            | true ->
                logger.Information($"发现插件 {plugin.PluginId} 的新版本 {remoteVer}")
                let targetPath = Path.Combine(cachePath, remotePackage.FileName)
                
                let! latestPackage = remoteRepo.GetLatestAsync(plugin.PluginId) |> Async.AwaitTask
                let! downloaded = storageRepository.DownloadFileAsync(latestPackage.FileUrl, targetPath, null)
                if downloaded then
                    try
                        let! _ = installer.InstallAsync(targetPath)
                        File.Delete(targetPath)
                        logger.Information($"插件 {plugin.PluginId} 更新成功")
                        do! notifyAppPluginUpdateAsync logger plugin.PluginId remotePackage.Version
                    with ex ->
                        logger.Error($"安装插件 {plugin.PluginId} 时失败 {ex}")
                else
                    logger.Warning($"插件 {plugin.PluginId} 下载失败")
            | false -> logger.Debug($"插件 {plugin.PluginId} 已是最新版本")

    with ex ->
        logger.Error($"插件更新流程发生异常{ex}")
}