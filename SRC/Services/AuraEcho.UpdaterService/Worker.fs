namespace AuraEcho.UpdaterService

open System
open System.IO
open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open AuraEcho.Cloud.V1
open AuraEcho.Core.Contracts
open AuraEcho.UpdaterService.Utils
open AuraEcho.UpdaterService.Workflows

type Worker(logger: ILogger<Worker>, scopeFactory: IServiceScopeFactory) =
    inherit BackgroundService()

    let paths = getAppPaths()

    override this.StartAsync(ct) =
        logger.LogInformation("Updater Service 正在初始化环境...")
        
        [paths.AppCache; paths.PluginCache]
        |> List.iter (fun p -> Directory.CreateDirectory(p) |> ignore)

        base.StartAsync(ct)

    override this.ExecuteAsync(stoppingToken: CancellationToken) = task {
        logger.LogInformation("Updater Service 工作循环已启动")
        
        use timer = new PeriodicTimer(TimeSpan.FromHours(1))
        
        while! timer.WaitForNextTickAsync(stoppingToken) do
            try
                // 每次循环创建一个独立的 DI Scope，确保 EF Core DbContext 的生命周期是正确的
                use scope = scopeFactory.CreateScope()
                let sp = scope.ServiceProvider

                // 解析依赖项
                let apiClient = sp.GetRequiredService<ApiClient>()
                let localPluginRepo = sp.GetRequiredService<ILocalPluginRepository>()
                let pluginInstaller = sp.GetRequiredService<IPluginInstallService>()

                do! updateAppAsync logger apiClient paths.AppCache
                do! updatePluginsAsync logger localPluginRepo apiClient pluginInstaller  paths.PluginCache
                
            with
            | :? OperationCanceledException ->
                logger.LogInformation("工作循环被取消")
            | ex ->
                logger.LogError(ex, "工作循环发生致命的未捕获异常")
    }

    override this.StopAsync(ct) =
        logger.LogInformation("Updater Service 正在停止...")
        base.StopAsync(ct)
