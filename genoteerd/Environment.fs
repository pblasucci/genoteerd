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
  let appFolder = DirectoryInfo(Path.Combine(basePath, ".genoteered"))
  do appFolder.Create()

  let logFolder = appFolder.CreateSubdirectory("logs")
  let logFileBase = logFolder.AppendPath("genoteerd_.txt")

  let dataFolder = appFolder.CreateSubdirectory("data")
  let storeFile = dataFolder.AppendPath(defaultArg dbFile "genoteerd.db")

  let connection =
    SQLiteConnectionStringBuilder(
      DataSource = storeFile,
      Version = 3,
      JournalMode = SQLiteJournalModeEnum.Wal
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
        path=logFileBase,
        rollingInterval=RollingInterval.Day,
        rollOnFileSizeLimit=true
      )
      .CreateLogger()

  /// Location on disk where all application files live.
  member _.AppFolder = appFolder

  /// Location on disk to which all log files are written.
  member _.LogFolder = logFolder

  /// Location on disk wherein all database files are keep.
  member _.DataFolder = dataFolder

  /// Full path to the database currently being used by the application.
  member _.StorageFile = FileInfo storeFile

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
