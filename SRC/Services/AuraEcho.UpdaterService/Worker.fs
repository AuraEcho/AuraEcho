namespace AuraEcho.UpdaterService

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open System.IO
open Microsoft.Extensions.DependencyInjection
open AuraEcho.Core.Contracts
open AuraEcho.PluginContracts.Interfaces
open Microsoft.Win32
open System.Diagnostics
open Microsoft.Data.Sqlite

type Worker(logger: IAppLogger, serviceProvider: IServiceProvider) =
    inherit BackgroundService()
    
    let basePath = 
        Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.CommonApplicationData, 
                     "AuraEcho", "UpdaterService", "Download")

    let appPackageCachePath = Path.Combine(basePath, "PackageCache")
    let pluginPackageCachePath = Path.Combine(basePath, "PluginCache")
    let configFilePath = Path.Combine(basePath, "pending_updates.json")

    do
        [basePath; appPackageCachePath; pluginPackageCachePath]
        |> List.iter (fun path -> path |> Directory.CreateDirectory |> ignore)

    let getRegistryValue registryKey = 
        use key = Registry.LocalMachine.OpenSubKey @"Software\AuraEcho"
        match key with
        | null -> None
        | _ -> key.GetValue registryKey |> Option.ofObj |> Option.map string

    let getInstallPath () = getRegistryValue "InstallPath"

    let getInstalledVersion () =
        getRegistryValue "CurrentVersion"
        |> Option.defaultValue "1.0.0"
        |> Version

    let isAppRunning () =
        let installFolder = getInstallPath() |> Option.map Path.GetDirectoryName
        let processNames = ["AuraEcho"; "PlixInstaller"]

        processNames
        |> List.collect (Process.GetProcessesByName >> List.ofArray)
        |> List.exists (fun p -> 
            try
                let pDir = Path.GetDirectoryName p.MainModule.FileName        
                installFolder = Some pDir && not p.HasExited    
            with _ -> false)
    let installAppPackage installerPath = async {
            logger.Information "开始启动客户端安装程序"
            let psi = ProcessStartInfo(installerPath, Arguments = "/quiet /log debug.log", UseShellExecute = false, CreateNoWindow = true)
            try
                use p = Process.Start psi
                match p |> Option.ofObj with
                | None -> logger.Information $"无法启动客户端安装程序 {installerPath}"
                | Some proc -> 
                    logger.Information $"客户端安装程序已启动，进程ID: {proc.Id}"
                    do! p.WaitForExitAsync() |> Async.AwaitTask
                    logger.Information "客户端更新完成"
                    if File.Exists installerPath then File.Delete installerPath

            with ex -> logger.Information $"安装客户端时发生异常: {ex.Message}"
        }
    let downloadAppPackage (packageRepo: IAppPackageRepository) = async {
        logger.Information "开始检测客户端版本信息..."
        let currentVersion = getInstalledVersion()
        let! latestInfo = packageRepo.GetLatestAsync() |> Async.AwaitTask
        let newestVersion = if isNull latestInfo then "1.0.0" else latestInfo.Version

        let newestVer = Version newestVersion

        if newestVer > currentVersion then
            logger.Information $"发现新版本 {newestVersion}，正在下载..."
            let targetPath = Path.Combine(appPackageCachePath, latestInfo.UpdateFileName)
            let! success = packageRepo.DownloadLatestAsync(false, targetPath, Progress<double> ignore) |> Async.AwaitTask
            match success with
            | true -> 
                logger.Information "客户端下载完成"
                do! installAppPackage targetPath
            | false -> 
                logger.Information "客户端下载失败"
        else
            logger.Information "未检测到更高版本的客户端"
    }
    let installPluginPackageCore packageFilePath (pluginName: string) = task {
        match getInstallPath() with
        | None -> logger.Warning "找不到客户端的安装目录，跳过插件安装"
        | Some installFolder -> 
            let pluginInstallerPath = Path.Combine(installFolder, "PluginInstaller.exe")
            let psi = ProcessStartInfo(pluginInstallerPath, UseShellExecute = false, CreateNoWindow = true)
            psi.ArgumentList.Add packageFilePath
            psi.ArgumentList.Add "--nowindow"

            try
                use p = Process.Start psi
                match p |> Option.ofObj with
                | None -> logger.Information $"无法启动插件安装程序 {pluginInstallerPath}"
                | Some proc -> 
                    logger.Information $"插件安装程序已启动，进程ID: {proc.Id}"
                    do! p.WaitForExitAsync() |> Async.AwaitTask
                    if File.Exists packageFilePath then File.Delete packageFilePath
                    logger.Information $"插件 {pluginName} 更新完成"
            with ex -> logger.Information $"安装插件 {pluginName} 时异常: {ex.Message}"
    }
    let updatePluginsAsync (localRepo: ILocalPluginRepository, remoteRepo: IRemotePluginRepository) = async {
        logger.Information "开始检测插件版本信息..."
        let! installedPlugins = localRepo.GetLocalPluginsAsync() |> Async.AwaitTask
        SqliteConnection.ClearAllPools()

        for plugin in installedPlugins do
            let! latestPackage = remoteRepo.GetLatestAsync plugin.Manifest.Id |> Async.AwaitTask
            let latestVersion = if isNull latestPackage then Version "0.0.0" else Version latestPackage.Version

            match latestVersion > Version plugin.Manifest.Version with
            | true -> 
                logger.Information $"发现 {plugin.Manifest.PluginName} 插件新版本 {latestVersion}，正在下载..."
                let targetPath = Path.Combine(pluginPackageCachePath, latestPackage.FileName)
                let! result = remoteRepo.DownloadLatestAsync(plugin.Manifest.Id, "stable", targetPath, Progress<double> ignore) |> Async.AwaitTask
                match result with
                | true -> 
                    logger.Information $"插件 {plugin.Manifest.PluginName} 下载完成"
                    logger.Information $"开始安装插件： {plugin.Manifest.PluginName}"
                    do! installPluginPackageCore targetPath plugin.Manifest.PluginName |> Async.AwaitTask
                | false -> logger.Information $"插件 {plugin.Manifest.PluginName} 下载失败"
            | false -> logger.Information $"插件 {plugin.Manifest.PluginName} 无需更新"
    }

    override _.ExecuteAsync(stoppingToken: CancellationToken) = task {
        logger.Information "更新服务工作循环已启动"
        
        while not stoppingToken.IsCancellationRequested do
            try
                use scope = serviceProvider.CreateScope()
                let packageRepo = scope.ServiceProvider.GetRequiredService<IAppPackageRepository>()
                let localRepo = scope.ServiceProvider.GetRequiredService<ILocalPluginRepository>()
                let remoteRepo = scope.ServiceProvider.GetRequiredService<IRemotePluginRepository>()

                do! downloadAppPackage packageRepo |> Async.StartAsTask
                
                do! updatePluginsAsync (localRepo, remoteRepo) |> Async.StartAsTask

                do! Task.Delay(TimeSpan.FromMinutes 1.0, stoppingToken)

            with ex ->
                logger.Information $"工作循环中发生错误: {ex.Message}"
    }

    override _.StartAsync ct = 
        logger.Information "Updater Service Starting..."
        base.StartAsync ct

    override _.StopAsync ct = 
        logger.Information "Updater Service Stopping..."
        base.StopAsync ct