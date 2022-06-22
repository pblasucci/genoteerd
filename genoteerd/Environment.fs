namespace MulberryLabs.Genoteerd

open System
open System.Data
open System.Data.SQLite
open System.IO
open NodaTime
open Serilog


/// Provides access and utilities for logging.
type ITrace =
  /// Emits a structured log entry for development-time diagnostics.
  abstract Debug : template : string * [<ParamArray>] data : obj array -> unit

  /// Emits a structured log entry for generally observable diagnostics.
  abstract Info : template : string * [<ParamArray>] data : obj array -> unit

  /// Emits a structured log entry when service is degraded,
  /// endangered, or may be behaving outside of its expected parameters.
  abstract Warn : template : string * [<ParamArray>] data : obj array -> unit

  /// Emits a structured log entry when essential functionality is unavailable.
  abstract Error :
    error : exn *
    template : string *
    [<ParamArray>] data : obj array
     -> unit


/// Provides access and utilities for clock, calender, and timezone.
type IClock =
  /// Return the current instant,
  /// as configured in a particular time zone (usually, system default).
  abstract JustNow : unit -> ZonedDateTime


/// Provides access and utilities for data persistence.
type IStore =
  /// Returns a new database connection.
  /// Callers are expected to handle closing/discarding the returned instance.
  abstract Connect : unit -> IDbConnection


[<AutoOpen>]
module Patterns =
  /// Helper to simplify working with IClock instances.
  let inline (|Trace|) (source : #ITrace) = Trace (source :> ITrace)

  /// Helper to simplify working with IClock instances.
  let inline (|Clock|) (source : #IClock) = Clock (source :> IClock)

  /// Helper to simplify working with IStore instances.
  let inline (|Store|) (source : #IStore) = Store (source :> IStore)


/// Provides execution-environment dependent data and functionality.
type AppEnv(basePath, zone, clock, ?dbFile) =
  let appFolder = Path.Combine(basePath, ".genoteered")
  let logFolder = Path.Combine(appFolder, "logs")
  let dataFolder = Path.Combine(appFolder, "data")
  let storeFile =
    dbFile |> Option.map (fun file -> Path.Combine(dataFolder, file))

  let connection =
    SQLiteConnectionStringBuilder(
      Version = 3,
      JournalMode = SQLiteJournalModeEnum.Wal,
      DataSource = (storeFile |> Option.defaultValue ":memory:")
    )

  let clock = ZonedClock(clock, zone, CalendarSystem.Iso)

  let logger =
    LoggerConfiguration()
#if DEBUG
      .MinimumLevel.Debug()
#else
      .MinimumLevel.Information()
#endif
      .WriteTo.File(
        path=Path.Combine(logFolder, "genoteerd_.txt"),
        rollingInterval=RollingInterval.Day,
        rollOnFileSizeLimit=true
      )
      .CreateLogger()

  do (* .ctor *)
    for folder in [ appFolder; logFolder; dataFolder ] do
      if not (Directory.Exists folder) then
        folder |> Directory.CreateDirectory |> ignore

  /// Location on disk where all application files live.
  member _.AppFolder = DirectoryInfo(appFolder)

  /// Location on disk to which all log files are written.
  member _.LogFolder = DirectoryInfo(logFolder)

  /// Location on disk wherein all database files are keep.
  member _.DataFolder = DirectoryInfo(dataFolder)

  /// Full path to the database currently being used by the application.
  member _.StorageFile = storeFile |> Option.map FileInfo

  interface ITrace with
    member _.Debug(template, data) = logger.Debug(template, data)
    member _.Info(template, data) = logger.Information(template, data)
    member _.Warn(template, data) = logger.Warning(template, data)
    member _.Error(error, template, data) = logger.Error(error, template, data)

  interface IClock with
    member _.JustNow() = clock.GetCurrentZonedDateTime()

  interface IStore with
    member _.Connect() =
      new SQLiteConnection(string connection) :> IDbConnection
