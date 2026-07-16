namespace AuraEcho.TelemetryService

open System
open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open AuraEcho.Cloud.V1
open AuraEcho.Core.Contracts
open AuraEcho.Core.Telemetry
open AuraEcho.PluginContracts.Interfaces
open AuraEcho.TelemetryService.Workflows

type Worker(logger: IAppLogger, scopeFactory: IServiceScopeFactory) =
    inherit BackgroundService()

    let options = FlushOptions.Default

    override this.ExecuteAsync(stoppingToken: CancellationToken) = task {
        logger.Information("Telemetry Service 工作循环已启动")

        use timer = new PeriodicTimer(options.FlushInterval)

        while! timer.WaitForNextTickAsync(stoppingToken) do
            try
                // 每轮创建独立 DI Scope，确保 EF Core DbContext 生命周期正确
                use scope = scopeFactory.CreateScope()
                let sp = scope.ServiceProvider

                let store = sp.GetRequiredService<TelemetryStore>()
                let apiClient = sp.GetRequiredService<ApiClient>()

                do! flushOnceAsync logger store apiClient options
            with
            | :? OperationCanceledException ->
                logger.Information("工作循环被取消")
            | ex ->
                logger.Error($"工作循环发生致命的未捕获异常: {ex}")
    }

    override this.StopAsync(ct) =
        logger.Information("Telemetry Service 正在停止...")
        base.StopAsync(ct)
