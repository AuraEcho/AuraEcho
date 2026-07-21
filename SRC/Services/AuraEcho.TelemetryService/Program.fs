namespace AuraEcho.TelemetryService

open System
open System.IO
open System.Diagnostics
open System.Net.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open AuraEcho.Cloud.V1
open AuraEcho.Core.Contracts
open AuraEcho.Logging
open AuraEcho.Core.Services
open AuraEcho.Core.Telemetry
open AuraEcho.Core.Tools.HttpClientPipelines

module Program =

    let logDir =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.CommonApplicationData,
            "AuraEcho",
            "TelemetryService",
            "Logs")

    let configureServices (services: IServiceCollection) =
        services.AddHostedService<Worker>()
                .AddSingleton<TelemetryContextFactory>()
                .AddSingleton<TelemetryStore>()
                .AddSingleton<ApiClient>(fun sp ->
                    let logHandler = new LoggingHandler(sp.GetRequiredService<ILogger<LoggingHandler>>(), InnerHandler = new HttpClientHandler())
                    new ApiClient(logHandler))
        |> ignore

    [<EntryPoint>]
    let main args =
        try
            Directory.CreateDirectory logDir |> ignore

            let loggingOptions = LoggingOptions(logDir, "telemetry-", "Telemetry")

            Host.CreateDefaultBuilder(args)
                .UseWindowsService(fun options -> options.ServiceName <- "AuraEcho Telemetry Service")
                .ConfigureLogging(fun lb -> lb.AddAuraEchoSerilog(loggingOptions) |> ignore)
                .ConfigureServices(configureServices)
                .Build()
                .Run()
            0
        with ex ->
            Debug.WriteLine($"服务启动失败: {ex.Message}")
            -1
