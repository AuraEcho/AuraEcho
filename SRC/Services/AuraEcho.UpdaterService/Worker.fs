namespace AuraEcho.UpdaterService

open System
open System.IO
open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open AuraEcho.Cloud.V1
open AuraEcho.Core.Contracts
open AuraEcho.PluginContracts.Interfaces
open AuraEcho.UpdaterService.Utils
open AuraEcho.UpdaterService.Workflows

type Worker(logger: IAppLogger, scopeFactory: IServiceScopeFactory) =
    inherit BackgroundService()

    let paths = getAppPaths()

    override this.StartAsync(ct) = 
        logger.Information("Updater Service 正在初始化环境...")
        
        [paths.AppCache; paths.PluginCache]
        |> List.iter (fun p -> Directory.CreateDirectory(p) |> ignore)

        base.StartAsync(ct)

    override this.ExecuteAsync(stoppingToken: CancellationToken) = task {
        logger.Information("Updater Service 工作循环已启动")
        
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
                logger.Information("工作循环被取消")
            | ex -> 
                logger.Error($"工作循环发生致命的未捕获异常: {ex}")
    }

    override this.StopAsync(ct) = 
        logger.Information("Updater Service 正在停止...")
        base.StopAsync(ct)
