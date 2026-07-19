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

/// 依次发送各批次
let rec private sendBatchesAsync (logger: IAppLogger) (store: TelemetryStore) (apiClient: ApiClient) (batches: TelemetryBatch list) = task {
    match batches with
    | [] -> return ()
    | batch :: rest ->
        let! status = apiClient.Telemetry.SendBatchAsync batch
        let ids = batch.Events |> Seq.map (fun e -> e.Id)

        match status with
        | TelemetryDeliveryStatus.Accepted ->
            store.Delete ids
            logger.Debug $"遥测批量发送成功: {batch.Events.Count} 条事件"
            return! sendBatchesAsync logger store apiClient rest
        | TelemetryDeliveryStatus.Rejected ->
            store.Delete ids
            logger.Warning $"遥测批量被服务端拒绝，已丢弃 {batch.Events.Count} 条事件"
            return! sendBatchesAsync logger store apiClient rest
        | _ ->
            // Deferred：暂时不可达，保留事件，中断本轮，等待下一轮
            logger.Debug "遥测暂时不可达，事件保留待重试"
            return ()
}

/// 从数据库缓存中获取遥测数据并发送
let flushOnceAsync (logger: IAppLogger) (store: TelemetryStore) (apiClient: ApiClient) (options: FlushOptions) = task {
    try
        // 超出条数上限时丢弃最旧事件
        store.TrimToCapacity options.MaxStoredEvents |> ignore

        let records = store.Dequeue options.BatchSize |> List.ofSeq
        if List.isEmpty records then
            return ()
        else
            let batches = groupByContext records
            do! sendBatchesAsync logger store apiClient batches
    with ex ->
        logger.Error $"遥测刷新过程中发生异常: {ex}"
}
