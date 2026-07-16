namespace AuraEcho.TelemetryService

open System
open System.IO
open System.Diagnostics
open System.Net.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open AuraEcho.Cloud.V1
open AuraEcho.Core.Contracts
open AuraEcho.Core.Services
open AuraEcho.Core.Telemetry
open AuraEcho.Core.Tools.HttpClientPipelines
open AuraEcho.PluginContracts.Interfaces

module Program =

    let logDir =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.CommonApplicationData,
            "AuraEcho",
            "TelemetryService",
            "Logs")

    let configureServices (services: IServiceCollection) =
        services.AddHostedService<Worker>()
                .AddSingleton<IAppLogger>(new Serilogger(logDir))
                .AddSingleton<TelemetryContextFactory>()
                .AddSingleton<TelemetryStore>()
                .AddSingleton<ApiClient>(fun sp ->
                    let logHandler = new LoggingHandler(sp.GetRequiredService<IAppLogger>(), InnerHandler = new HttpClientHandler())
                    new ApiClient(logHandler))
        |> ignore

    [<EntryPoint>]
    let main args =
        try
            Directory.CreateDirectory logDir |> ignore

            Host.CreateDefaultBuilder(args)
                .UseWindowsService(fun options -> options.ServiceName <- "AuraEcho Telemetry Service")
                .ConfigureServices(configureServices)
                .Build()
                .Run()
            0
        with ex ->
            Debug.WriteLine($"服务启动失败: {ex.Message}")
            -1
