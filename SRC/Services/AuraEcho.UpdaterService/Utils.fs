module AuraEcho.UpdaterService.Utils

open System
open System.IO
open System.Diagnostics
open Microsoft.Win32

// 基础配置记录
let getAppPaths () =
    let baseData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
    let rootPath = Path.Combine(baseData, "AuraEcho", "UpdaterService")
    {|
        LogDir = Path.Combine(rootPath, "Logs")
        AppCache = Path.Combine(rootPath, "Download", "PackageCache")
        PluginCache = Path.Combine(rootPath, "Download", "PluginCache")
    |}

// 客户端信息
module SystemInfo =
    let private getRegistryValue keyName = 
        use key = Registry.LocalMachine.OpenSubKey(@"Software\AuraEcho")
        if isNull key then None 
        else key.GetValue(keyName) |> Option.ofObj |> Option.map string

    let getInstallPath () = getRegistryValue "InstallPath"
    
    let getInstalledVersion () =
        getRegistryValue "CurrentVersion" 
        |> Option.defaultValue "1.0.0" 
        |> Version

    let isAppRunning () =
        let targetDir = getInstallPath() |> Option.map Path.GetDirectoryName
        
        [ProcessNames.HostProcess; ProcessNames.PluginInstaller]
        |> List.collect (Process.GetProcessesByName >> List.ofArray)
        |> List.exists (fun p -> 
            try
                let pDir = Path.GetDirectoryName(p.MainModule.FileName)
                targetDir = Some pDir && not p.HasExited    
            with _ -> false)

// 进程执行工具
module ProcessHelper =
    let runInstallerAsync installerPath = task {
        let psi = ProcessStartInfo(
            FileName = installerPath, 
            Arguments = "/quiet /log debug.log", 
            UseShellExecute = false, 
            CreateNoWindow = true)
            
        try
            use p = Process.Start(psi)
            if isNull p then return Error "进程启动失败"
            else
                do! p.WaitForExitAsync()
                return Ok ()
        with ex -> 
            return Error ex.Message
    }