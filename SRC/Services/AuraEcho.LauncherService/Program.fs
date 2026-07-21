namespace AuraEcho.LauncherService

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open System.IO
open AuraEcho.Logging

module Program =
    let logDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "AuraEcho",
        "LauncherService",
        "Logs")

    [<EntryPoint>]
    let main args =

        let loggingOptions = LoggingOptions(logDir, "launcher-", "Launcher")

        Host.CreateDefaultBuilder(args)
            .UseWindowsService(fun opt -> opt.ServiceName <- "AuraEchoLauncherService")
            .ConfigureLogging(fun lb -> lb.AddAuraEchoSerilog(loggingOptions) |> ignore)
            .ConfigureServices(fun services -> services.AddHostedService<LauncherWorker>() |> ignore)
            .Build()
            .Run()
        0