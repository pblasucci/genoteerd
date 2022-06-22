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
    if isNull zone then failwith "Unable to determine current time zone!"
    zone

  let ensureBasePath () =
    match GetFolderPath(SpecialFolder.Personal) with
    | Length 0u -> failwith "Unable to determine home folder."
    | folder -> folder

  let ensureDatabase (Trace log as env:AppEnv) =
    let needsInstall, filePath =
      env.StorageFile
      |> Option.map (fun file ->
          let newStore = not file.Exists
          if newStore then
            using (file.Open(FileMode.OpenOrCreate)) ignore
            file.Refresh()
          newStore, file.FullName
      )
      |> Option.defaultValue (true, ":memory:") // ⮜⮜⮜ in-memory storage

    if needsInstall then
      match DDL.install env with
      | Ok () -> log.Info("New store installed at '{Path}'", filePath)
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

  override me.Initialize() =
    AvaloniaXamlLoader.Load(me)

  override me.OnFrameworkInitializationCompleted() =
    match me.ApplicationLifetime with
    | :? IClassicDesktopStyleApplicationLifetime as desktop ->

      desktop.ShutdownMode <- ShutdownMode.OnLastWindowClose

      let env = AppEnv(
        ensureBasePath (),
        ensureTimeZone (),
        SystemClock.Instance,
        dbFile="develop.db" //TODO optional CLI arg
      )
      ensureDatabase env
      launchNotes env

    | _ -> failwith "Incorrect application lifetime detected."

    base.OnFrameworkInitializationCompleted()


module Program =
  [<EntryPoint>]
  let main args =
    try
      AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .UseSkia()
        .StartWithClassicDesktopLifetime(args)
    with x ->
      MessageBox.Alert(x.Message, "Critical Failure!")
      1 // ⮜⮜⮜ non-success exit code
