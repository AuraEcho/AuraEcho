namespace AuraEcho.UpdaterService

open System
open System.IO
open System.Diagnostics
open System.Net.Http
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open AuraEcho.Cloud.Helpers
open AuraEcho.Cloud.V1
open AuraEcho.Cloud.V1.EndPoints
open AuraEcho.Core.Contracts
open AuraEcho.Core.Data
open AuraEcho.Logging
open AuraEcho.Core.Repositories
open AuraEcho.Core.Services
open AuraEcho.Core.Tools
open AuraEcho.Core.Tools.HttpClientPipelines
open AuraEcho.PluginContracts.Interfaces
open AuraEcho.Telemetry

module Program =

    let logDir =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.CommonApplicationData,
            "AuraEcho",
            "UpdaterService",
            "Logs");

    let configureServices (services: IServiceCollection) =
        services.AddHostedService<Worker>()
                .AddDbContext<HostDbContext>(fun options ->
                    options.UseSqlite $"Data Source={ApplicationPaths.HostDataBase}" |> ignore)
                .AddSingleton<HttpClient>(fun sp ->
                    let logHandler = new LoggingHandler(sp.GetRequiredService<ILogger<LoggingHandler>>(), InnerHandler = new HttpClientHandler())
                    new HttpClient(logHandler))
                .AddSingleton<HttpHelper>(fun sp ->
                    let client = sp.GetRequiredService<HttpClient>()
                    HttpHelper(client))
                .AddSingleton<ApiClient>(fun sp ->
                    let logHandler = new LoggingHandler(sp.GetRequiredService<ILogger<LoggingHandler>>(), InnerHandler = new HttpClientHandler())
                    new ApiClient(logHandler))
                .AddSingleton<TelemetryContextFactory>(fun sp ->
                    let idProvider = fun () -> Guid.NewGuid()
                    new TelemetryContextFactory(idProvider))
                .AddSingleton<TelemetryStore>(fun sp ->
                    new TelemetryStore(ApplicationPaths.TelemetryDataBase))
                .AddSingleton<AuraEcho.Telemetry.ITelemetryService, TelemetryService>()
                .AddScoped<ILocalPluginRepository, LocalPluginRepository>()
                .AddScoped<IPluginInstallService, PluginInstallService>()
        |> ignore

    [<EntryPoint>]
    let main args =
        try
            Directory.CreateDirectory logDir |> ignore;

            let loggingOptions = LoggingOptions(logDir, "updater-", "Updater")

            let builder =
                Host.CreateDefaultBuilder(args)
                    .UseWindowsService(fun options -> options.ServiceName <- "AuraEcho Updater Service")
                    .ConfigureLogging(fun lb -> lb.AddAuraEchoSerilog(loggingOptions) |> ignore)
                    .ConfigureServices(configureServices)
                    .Build()
                    .Run()
            0
        with ex ->
            Debug.WriteLine($"服务启动失败: {ex.Message}")
            -1
