open System
open System.IO
open Microsoft.EntityFrameworkCore
open AuraEcho.Persistence
open AuraEcho.Telemetry

let migrate (dbContext: DbContext) =
    let pending = dbContext.Database.GetPendingMigrations()
    if Seq.isEmpty pending |> not then
        dbContext.Database.Migrate() |> ignore

[<EntryPoint>]
let main _ =

    let hostDbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "AuraEcho", "Client", "Data", "host.db")

    let hostDbProvider = HostDbContextProvider(hostDbPath)
    use auraEchoDbContext = hostDbProvider.CreateDbContext()
    migrate auraEchoDbContext

    let telemetryDbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "AuraEcho", "Client", "Data", "telemetry.db")

    let telemetryOptions =
        DbContextOptionsBuilder<TelemetryDbContext>()
            .UseSqlite($"Data Source={telemetryDbPath}")
            .Options

    use telemetryDbContext = new TelemetryDbContext(telemetryOptions)
    migrate telemetryDbContext

    0
