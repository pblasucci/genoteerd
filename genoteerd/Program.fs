﻿namespace MulberryLabs.Genoteerd

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
    | Length 0u -> invalidProgram "Unable to determine home folder."
    | folder -> folder

  let tryGetDbFile args =
    match args with
    | [| "--db"; Length n as file |]
      when 0u < n -> Some file
    | _otherwise  -> None

  let ensureDatabase (Trace log as env:AppEnv) =
    if not env.StorageFile.Exists then
      using (env.StorageFile.Open FileMode.OpenOrCreate) ignore
      env.StorageFile.Refresh()

      match DDL.install env with
      | Ok () ->
          let path = env.StorageFile.FullName
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

      desktop.ShutdownMode <- ShutdownMode.OnLastWindowClose

      let env = AppEnv(
        ensureBasePath (),
        ensureTimeZone (),
        SystemClock.Instance,
        ?dbFile=tryGetDbFile desktop.Args
      )

      ensureDatabase env
      launchNotes env

    | _ -> invalidProgram "Incorrect application lifetime detected."

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