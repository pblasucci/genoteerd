namespace MulberryLabs.Genoteerd

open System.IO
open NodaTime
open Serilog
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open MulberryLabs.Genoteerd.Storage

open type System.Environment
open type RollingInterval


type App() =
  inherit Application()

  let ensureTimeZone () =
    let zone = DateTimeZoneProviders.Tzdb.GetSystemDefault()
    if isNull zone then invalidProg "Unable to determine current time zone!"
    zone

  let ensureBasePath () =
    match GetFolderPath(SpecialFolder.Personal) with
    | Length 0u -> invalidProg "Unable to determine home folder."
    | folder -> folder

  let tryGetDbFile args =
    match args with
    | [| "--db"; Length n as file |]
      when 0u < n -> Some file
    | _otherwise  -> None

  let ensureDatabase (Trace log & Store db) =
    if not db.StorageFile.Exists then
      using (db.StorageFile.Open FileMode.OpenOrCreate) ignore
      db.StorageFile.Refresh()

      match DDL.install db with
      | Ok () ->
          let path = db.StorageFile.FullName
          log.Info("New store installed at '{Path}'", path)
      | Error x -> raise x

  let launchNotes (Trace log as env) =
    match SQL.selectAllNotes env with
    | Error failure ->
        log.Error(failure, "Unable to retrieve existing notes.")
        StickyNoteHost(env).Show()
    | Ok [] -> StickyNoteHost(env).Show()
    | Ok notes ->
        for note in notes do
          StickyNoteHost(env, note).Show()

  override me.Initialize() = AvaloniaXamlLoader.Load(me)

  override me.OnFrameworkInitializationCompleted() =
    match me.ApplicationLifetime with
    | :? IClassicDesktopStyleApplicationLifetime as desktop ->
      let env = AppEnv(
        ensureBasePath (),
        ensureTimeZone (),
        SystemClock.Instance,
        ?dbFile=tryGetDbFile desktop.Args
      )

      ensureDatabase env
      launchNotes env

    | _ -> invalidProg "Incorrect application lifetime detected."

    base.OnFrameworkInitializationCompleted()


module Program =
  [<EntryPoint>]
  let main args =
    try
      AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .UseSkia()
        .StartWithClassicDesktopLifetime(args, ShutdownMode.OnLastWindowClose)
    with x ->
      MessageBox.Alert(x.Message, title = "Critical Failure!")
      1 // ⮜⮜⮜ non-success exit code
