module AuraEcho.TelemetryService.Workflows

open System
open AuraEcho.Cloud.V1
open AuraEcho.Cloud.V1.Models.Telemetry
open AuraEcho.Core.Contracts
open AuraEcho.Core.Telemetry
open AuraEcho.PluginContracts.Interfaces

/// 遥测刷新配置。
type FlushOptions =
    { FlushInterval: TimeSpan
      BatchSize: int
      MaxRetries: int }

    static member Default =
        { FlushInterval = TimeSpan.FromSeconds 30.0
          BatchSize = 50
          MaxRetries = 5 }

/// 将一条扁平存储记录映射为上传用的事件线路模型。
let private toEvent (r: TelemetryEventRecord) =
    TelemetryEvent(
        Id = r.Id,
        Timestamp = r.Timestamp,
        Type = r.Type,
        Name = r.Name,
        Culture = r.Culture,
        Properties = r.Properties,
        Metrics = r.Metrics)

// 从一批同源记录还原出上传用的会话上下文
let private toBatch (records: TelemetryEventRecord list) =
    let head = List.head records
    let context =
        TelemetryContext(
            InstallationId = head.InstallationId,
            AppVersion = head.AppVersion,
            OSVersion = head.OSVersion,
            NetVersion = head.NetVersion,
            SessionId = head.SessionId)
    TelemetryBatch(
        Context = context,
        Events = ResizeArray(records |> List.map toEvent),
        SentAt = DateTime.UtcNow)

// 按产生事件时快照的会话上下文分组
let private groupByContext (records: TelemetryEventRecord list) =
    records
    |> List.groupBy (fun r ->
        r.InstallationId, r.AppVersion, r.OSVersion, r.NetVersion, r.SessionId)
    |> List.map (snd >> toBatch)

// 上传单个批次
let private sendBatchAsync (apiClient: ApiClient) (batch: TelemetryBatch) = task {
    let! success = apiClient.Telemetry.SendBatchAsync batch
    return success, batch
}

/// 从数据库缓存中获取遥测数据并发送
let flushOnceAsync (logger: IAppLogger) (store: TelemetryStore) (apiClient: ApiClient) (options: FlushOptions) = task {
    try
        // 清理重试超限的废弃事件
        store.PurgeDeadEvents options.MaxRetries |> ignore

        let records = store.Dequeue options.BatchSize |> List.ofSeq
        if List.isEmpty records then
            return ()
        else
            let batches = groupByContext records

            for batch in batches do
                let! success, sent = sendBatchAsync apiClient batch
                let ids = sent.Events |> Seq.map (fun e -> e.Id)

                if success then
                    store.Delete ids
                    logger.Debug $"遥测批量发送成功: {sent.Events.Count} 条事件"
                else
                    store.IncrementRetryCount ids
                    logger.Warning "遥测批量发送失败，已标记重试"
    with ex ->
        logger.Error $"遥测刷新过程中发生异常: {ex}"
}
