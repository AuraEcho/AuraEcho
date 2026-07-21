namespace AuraEcho.TelemetryRelayService

open System
open System.IO
open System.Diagnostics
open System.Net.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open AuraEcho.Cloud.V1
open AuraEcho.Logging
open AuraEcho.Telemetry

module Program =

    let logDir =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.CommonApplicationData,
            "AuraEcho",
            "TelemetryRelayService",
            "Logs")

    let configureServices (services: IServiceCollection) =
        let dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "AuraEcho", "Client", "Data", "telemetry.db")

        services.AddHostedService<Worker>()
                .AddSingleton<TelemetryStore>(new TelemetryStore(dbPath))
                .AddSingleton<TelemetryContextFactory>(fun sp ->
                    // 遥测服务端不需要 InstallationId 持久化，每次生成新 ID 即可
                    new TelemetryContextFactory(fun () -> Guid.NewGuid()))
                .AddSingleton<ApiClient>(fun sp ->
                    let logHandler = new LoggingHandler(sp.GetRequiredService<ILogger<LoggingHandler>>(), InnerHandler = new HttpClientHandler())
                    new ApiClient(logHandler))
        |> ignore

    [<EntryPoint>]
    let main args =
        try
            Directory.CreateDirectory logDir |> ignore

            let loggingOptions = LoggingOptions(logDir, "telemetry-relay-", "TelemetryRelay")

            Host.CreateDefaultBuilder(args)
                .UseWindowsService(fun options -> options.ServiceName <- "AuraEcho Telemetry Relay Service")
                .ConfigureLogging(fun lb -> lb.AddAuraEchoSerilog(loggingOptions) |> ignore)
                .ConfigureServices(configureServices)
                .Build()
                .Run()
            0
        with ex ->
            Debug.WriteLine($"服务启动失败: {ex.Message}")
            -1
