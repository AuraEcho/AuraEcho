module AuraEcho.UpdaterService.Workflows

open System
open System.IO
open Microsoft.Extensions.Logging
open AuraEcho.Core.Contracts
open AuraEcho.PluginContracts.Interfaces
open AuraEcho.UpdaterService.Utils
open Microsoft.Data.Sqlite

// --- 客户端更新 ---
let updateAppAsync (logger: IAppLogger) (repo: IAppPackageRepository) cachePath = task {
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
            
            let! downloaded = repo.DownloadLatestAsync(false, targetPath, null)
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

// --- 插件更新 ---
let updatePluginsAsync (logger: IAppLogger) (localRepo: ILocalPluginRepository) (remoteRepo: IRemotePluginRepository) (installer: IPluginInstallService) cachePath = task {
    logger.Information("开始检测插件版本...")
    
    try
        let! installedPlugins = localRepo.GetLocalPluginsAsync()
        SqliteConnection.ClearAllPools()

        for plugin in installedPlugins do
            let pluginName = plugin.Manifest.PluginName
            let! remotePackage = remoteRepo.GetLatestAsync(plugin.Manifest.Id)
            let remoteVer = if isNull remotePackage then Version("0.0.0") else Version(remotePackage.Version)
            let localVer = Version(plugin.Manifest.Version)

            match remoteVer > localVer with
            | true ->
                logger.Information($"发现插件 {pluginName} 的新版本 {remoteVer}")
                let targetPath = Path.Combine(cachePath, remotePackage.FileName)
                
                let! downloaded = remoteRepo.DownloadLatestAsync(plugin.Manifest.Id, "stable", targetPath, null)
                if downloaded then
                    try
                        let! _ = installer.InstallAsync(targetPath)
                        File.Delete(targetPath)
                        logger.Information($"插件 {pluginName} 更新成功")
                    with ex ->
                        logger.Error($"安装插件 {pluginName} 时失败 {ex}")
                else
                    logger.Warning($"插件 {pluginName} 下载失败")
            | false -> logger.Debug($"插件 {pluginName} 已是最新版本")

    with ex ->
        logger.Error($"插件更新流程发生异常{ex}")
}