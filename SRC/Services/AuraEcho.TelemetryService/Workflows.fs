module AuraEcho.TelemetryService.Workflows

open System
open AuraEcho.Cloud.V1
open AuraEcho.Cloud.V1.Models.Telemetry
open AuraEcho.Core.Contracts
open AuraEcho.Core.Telemetry
open Microsoft.Extensions.Logging

/// 遥测刷新配置。
type FlushOptions =
    { FlushInterval: TimeSpan
      BatchSize: int
      // 本地缓存条数上限，超出后按最旧丢弃，防止长期离线导致本地库无限膨胀。
      MaxStoredEvents: int }

    static member Default =
        { FlushInterval = TimeSpan.FromSeconds 30.0
          BatchSize = 50
          MaxStoredEvents = 10000 }

/// 将一条扁平存储记录映射为上传用的事件线路模型。
let private toEvent (r: TelemetryEventRecord) =
    TelemetryEvent(
        Id = r.Id,
        Timestamp = r.Timestamp,
        Type = r.Type,
        Name = r.Name,
        SessionId = r.SessionId,
        SequenceNumber = r.SequenceNumber,
        Properties = r.Properties,
        Metrics = r.Metrics)

// 将一批记录打包为上传批次
let private toBatch (records: TelemetryEventRecord list) =
    TelemetryBatch(
        Events = ResizeArray(records |> List.map toEvent),
        SentAt = DateTime.UtcNow)

/// 发送单个批次
let private sendBatchAsync (logger: ILogger) (store: TelemetryStore) (apiClient: ApiClient) (batch: TelemetryBatch) = task {
    let! status = apiClient.Telemetry.SendBatchAsync batch
    let ids = batch.Events |> Seq.map (fun e -> e.Id)

    match status with
    | TelemetryDeliveryStatus.Accepted ->
        store.Delete ids
        logger.LogDebug("遥测批量发送成功: {EventCount} 条事件", batch.Events.Count)
    | TelemetryDeliveryStatus.Rejected ->
        store.Delete ids
        logger.LogWarning("遥测批量被服务端拒绝，已丢弃 {EventCount} 条事件", batch.Events.Count)
    | _ ->
        logger.LogDebug("遥测暂时不可达，事件保留待重试")
}

/// 从数据库缓存中获取遥测数据并发送
let flushOnceAsync (logger: ILogger) (store: TelemetryStore) (apiClient: ApiClient) (options: FlushOptions) = task {
    try
        // 超出条数上限时丢弃最旧事件
        store.TrimToCapacity options.MaxStoredEvents |> ignore

        let records = store.Dequeue options.BatchSize |> List.ofSeq
        if List.isEmpty records then
            return ()
        else
            let batch = toBatch records
            do! sendBatchAsync logger store apiClient batch
    with ex ->
        logger.LogError(ex, "遥测刷新过程中发生异常")
}
