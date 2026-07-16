open Microsoft.EntityFrameworkCore
open AuraEcho.Core.Data

let migrate (dbContext: DbContext) =
    let pending = dbContext.Database.GetPendingMigrations()
    if Seq.isEmpty pending |> not then
        dbContext.Database.Migrate() |> ignore

[<EntryPoint>]
let main _ =

    use auraEchoDbContext = HostDbContextRuntimeFactory.CreateDbContext()
    migrate auraEchoDbContext

    use telemetryDbContext = TelemetryDbContextRuntimeFactory.CreateDbContext()
    migrate telemetryDbContext

    0
