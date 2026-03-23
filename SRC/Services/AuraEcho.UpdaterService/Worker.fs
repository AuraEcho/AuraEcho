namespace AuraEcho.UpdaterService

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open System.IO
open AuraEcho.Core.Models
open Microsoft.Extensions.DependencyInjection
open AuraEcho.Core.Contracts
open AuraEcho.PluginContracts.Interfaces
open Microsoft.Win32
open System.Diagnostics
open Microsoft.Data.Sqlite
open System.Text.Json

[<CLIMutable>]
type PluginUpdateInfo = {
    PluginId: Guid
    Version: string
    FilePath: string
}

[<CLIMutable>]
type PendingUpdate = {
    mutable Plugins: Map<Guid, PluginUpdateInfo>
}

type Worker(logger: IAppLogger, serviceProvider: IServiceProvider) =
    inherit BackgroundService()
    
    let basePath = 
        Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.CommonApplicationData, 
                     "AuraEcho", "UpdaterService", "Download")

    let appPackageCachePath = Path.Combine(basePath, "PackageCache")
    let pluginPackageCachePath = Path.Combine(basePath, "PluginCache")
    let configFilePath = Path.Combine(basePath, "pending_updates.json")
    
    let mutable pendingUpdate = { Plugins = Map.empty }

    let saveState (state: PendingUpdate) =
        try
            let options = JsonSerializerOptions(WriteIndented = true)
            let json = JsonSerializer.Serialize(state, options)
            File.WriteAllText(configFilePath, json)
        with ex -> logger.Information $"保存状态文件失败: {ex.Message}"

    let loadState () =
        try
            if File.Exists configFilePath then
                let json = File.ReadAllText(configFilePath)
                pendingUpdate <- JsonSerializer.Deserialize<PendingUpdate>(json)
                logger.Information "成功从本地恢复待更新状态"
        with ex -> 
            logger.Information $"加载状态文件失败，将重新开始: {ex.Message}"
            pendingUpdate <- { Plugins = Map.empty }

    do
        [basePath; appPackageCachePath; pluginPackageCachePath]
        |> List.iter (fun path -> path |> Directory.CreateDirectory |> ignore)
        loadState()

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
                return Some targetPath
            | false -> 
                logger.Information "客户端下载失败"
                return None
        else
            logger.Information "未检测到更高版本的客户端"
            return None
    }

    let downloadPluginPackage (localRepo: ILocalPluginRepository, remoteRepo: IRemotePluginRepository) = async {
        logger.Information "开始检测插件版本信息..."
        let installedPlugins = 
            localRepo.GetPluginRegistries() 
            |> List.ofSeq 
            |> List.filter (fun p -> p.PlanStatus <> PluginPlanStatus.UninstallPending)

        SqliteConnection.ClearAllPools()

        for plugin in installedPlugins do
            let! latestPackage = remoteRepo.GetLatestAsync plugin.Manifest.Id |> Async.AwaitTask
            let latestVersion = if isNull latestPackage then Version "0.0.0" else Version latestPackage.Version
            
            let cachedVersion = 
                match pendingUpdate.Plugins.TryFind plugin.Manifest.Id with
                | Some info -> Version info.Version
                | None -> Version "0.0.0"

            if latestVersion > Version plugin.Manifest.Version && latestVersion > cachedVersion then
                logger.Information $"发现 {plugin.Manifest.PluginName} 插件新版本 {latestVersion}，正在下载..."
                let targetPath = Path.Combine(pluginPackageCachePath, latestPackage.FileName)
                let! result = remoteRepo.DownloadLatestAsync(plugin.Manifest.Id, "stable", targetPath, Progress<double> ignore) |> Async.AwaitTask
                match result with
                | true -> 
                    let newPlugins = pendingUpdate.Plugins.Add(plugin.Manifest.Id, { PluginId = plugin.Manifest.Id; Version = latestPackage.Version; FilePath = targetPath})
                    pendingUpdate.Plugins <- newPlugins
                    saveState pendingUpdate
                    logger.Information $"插件 {plugin.Manifest.PluginName} 下载完成"
                | false -> logger.Information $"插件 {plugin.Manifest.PluginName} 下载失败"
    }

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

    let installPluginPackageCore (pluginIds: Guid list) installFolder = task {
        let pluginInstallerPath = Path.Combine(installFolder, "PluginInstaller.exe")

        for pluginId in pluginIds do
            match pendingUpdate.Plugins.TryFind pluginId with
            | Some info ->
                logger.Information $"正在更新插件: {pluginId}"
                let psi = ProcessStartInfo(pluginInstallerPath, UseShellExecute = false, CreateNoWindow = true)
                psi.ArgumentList.Add info.FilePath
                psi.ArgumentList.Add "--nowindow"

                try
                    use p = Process.Start psi
                    match p |> Option.ofObj with
                    | None -> logger.Information $"无法启动插件安装程序 {pluginInstallerPath}"
                    | Some proc -> 
                        logger.Information $"插件安装程序已启动，进程ID: {proc.Id}"
                        do! p.WaitForExitAsync() |> Async.AwaitTask
                        if File.Exists info.FilePath then File.Delete info.FilePath
                        pendingUpdate.Plugins <- pendingUpdate.Plugins.Remove pluginId
                        saveState pendingUpdate
                        logger.Information $"插件 {pluginId} 更新完成"

                with ex -> logger.Information $"安装插件 {pluginId} 时异常: {ex.Message}"
            | None -> ()
    }

    let installPluginPackage (localRepo: ILocalPluginRepository) = async {

        let cachedPluginIds = pendingUpdate.Plugins.Keys 
        if Seq.isEmpty cachedPluginIds then return ()

        let installedPluginIds = 
            localRepo.GetPluginRegistries()
            |> Seq.filter (fun p -> p.PlanStatus <> PluginPlanStatus.UninstallPending)
            |> Seq.map (fun p -> p.Manifest.Id)
            |> Set.ofSeq
        SqliteConnection.ClearAllPools()

        let needUpdatePlugins = 
            cachedPluginIds 
            |> Seq.filter installedPluginIds.Contains
            |> Seq.toList 

        if List.isEmpty needUpdatePlugins then 
            return ()

        match getInstallPath() with
        | None -> 
            logger.Warning "找不到客户端的安装目录，跳过插件安装"
        | Some installFolder -> 
            try
                do! installPluginPackageCore needUpdatePlugins installFolder 
                    |> Async.AwaitTask
            with
            | ex -> 
                logger.Error($"执行插件包安装时发生异常{ex}")
    }

    override _.ExecuteAsync(stoppingToken: CancellationToken) = task {
        logger.Information "更新服务工作循环已启动"
        
        while not stoppingToken.IsCancellationRequested do
            try
                use scope = serviceProvider.CreateScope()
                let packageRepo = scope.ServiceProvider.GetRequiredService<IAppPackageRepository>()
                let localRepo = scope.ServiceProvider.GetRequiredService<ILocalPluginRepository>()
                let remoteRepo = scope.ServiceProvider.GetRequiredService<IRemotePluginRepository>()

                let! downloadResult = downloadAppPackage packageRepo |> Async.StartAsTask
                match downloadResult with
                | Some installerPath -> do! installAppPackage installerPath |> Async.StartAsTask
                | None -> ()

                do! downloadPluginPackage (localRepo, remoteRepo) |> Async.StartAsTask

                if not (isAppRunning()) then
                    do! installPluginPackage localRepo |> Async.StartAsTask
                else
                    if not pendingUpdate.Plugins.IsEmpty then
                        logger.Information "检测到待更新项，但程序正在运行，等待退出后安装"

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