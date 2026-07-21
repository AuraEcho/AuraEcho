namespace AuraEcho.TelemetryService

open System
open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open AuraEcho.Cloud.V1
open AuraEcho.Telemetry
open AuraEcho.TelemetryService.Workflows

type Worker(logger: ILogger<Worker>, scopeFactory: IServiceScopeFactory) =
    inherit BackgroundService()

    let options = FlushOptions.Default

    override this.ExecuteAsync(stoppingToken: CancellationToken) = task {
        logger.LogInformation("Telemetry Service 工作循环已启动")

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
                logger.LogInformation("工作循环被取消")
            | ex ->
                logger.LogError(ex, "工作循环发生致命的未捕获异常")
    }

    override this.StopAsync(ct) =
        logger.LogInformation("Telemetry Service 正在停止...")
        base.StopAsync(ct)
