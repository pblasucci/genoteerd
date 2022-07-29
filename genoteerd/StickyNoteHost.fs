namespace MulberryLabs.Genoteerd

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Hosts
open Avalonia.Platform

open MulberryLabs.Genoteerd
open MulberryLabs.Genoteerd.Storage


type StickyNoteHost(env: AppEnv, ?note : Note, ?theme : NoteTheme) as me =
  inherit HostWindow()

  let host = me :> IStickyNoteHost
  // HACK ⮝⮝⮝ makes it easier to call into myself elsewhere
  let (Trace log) = env
  // HACK ⮝⮝⮝ remember: active patterns cannot be used in a .ctor

  let mutable note' =
    note |> Option.defaultWith (fun () -> Note.New(?theme=theme))

  let extractTitle = function
    | Length 0u     -> "Genoteerd"
    | Length n as text
      when 128u < n -> $"Genoteerd - %s{text[.. 125]}..."
    | otherwise     -> $"Genoteerd - %s{otherwise}"

  do (* .ctor *)
    let startLocation, top, right, bottom, left =
      match note with
      | None ->
        let start = WindowStartupLocation.CenterScreen
        (start, None, None, None, None)
      | Some n ->
        let start = WindowStartupLocation.Manual
        let geo = n.Geometry
        (start, Some geo.Y, Some geo.Width, Some geo.Height, Some geo.X)

    me.SystemDecorations <- SystemDecorations.None
    me.ExtendClientAreaChromeHints <- ExtendClientAreaChromeHints.NoChrome
    me.WindowStartupLocation <- startLocation
    me.ShowInTaskbar <- true
    me.MinHeight <- 120.
    me.MinWidth <- 120.
    me.Width <- defaultArg right 120.
    me.Height <- defaultArg bottom 120.
    me.Title <- extractTitle note'.Content
    me.Content <- StickyNoteView.main host note'.Content
    me.Classes.Add(string note'.Theme)

    me.Opened.Add(fun _ ->
      let pos = me.Position
      me.Position <-
        match left, top with
        | Some x, Some y  -> PixelPoint(int x, int y)
        | None  , Some y  -> PixelPoint(pos.X, int y)
        | Some x, None    -> PixelPoint(int x, pos.Y)
        | None  , None    -> (* no-op *) pos
    )

    // geometric event handling
    me.PointerPressed.Add(fun args ->
      if args.Pointer.IsPrimary then
        args.Handled <- true
        // resizing
        match args.Source with
        | :? WrapPanel as edge when edge.Classes.Contains "edge" ->
          let windowEdge =
            match DockPanel.GetDock(edge) with
            | Dock.Top    -> WindowEdge.North
            | Dock.Right  -> WindowEdge.East
            | Dock.Bottom -> WindowEdge.South
            | Dock.Left   -> WindowEdge.West
            | otherwise ->
              failwith $"Unknown Dock enumeration value: {otherwise}"
          me.BeginResizeDrag(windowEdge, args)
        // moving
        | _ -> me.BeginMoveDrag(args)
    )

    // NOTE ⮟⮟⮟ fires when window dragged -or- top / left edge resized
    me.PositionChanged.Add(fun _ -> host.Upsert(note'.Content))

    // NOTE ⮟⮟⮟ fires when any edge resized
    // NOTE ⮟⮟⮟ only reports rect of window, not location on screen
    me.EffectiveViewportChanged.Add(fun _ -> host.Upsert(note'.Content))

  interface IStickyNoteHost with
    member _.Quit() =
      match Application.CurrentDesktop with
      | Some desktop -> desktop.Shutdown()
      | None -> invalidProg "Incorrect application lifetime detected."

    member _.Launch() =
      StickyNoteHost(env, theme=note'.Theme).Show()

    member me.Upsert(content, ?theme) =
      let origin = me.Position
      let update = {
        note' with
          Content = content
          Geometry = Rect(float origin.X, float origin.Y, me.Width, me.Height)
          Theme = defaultArg theme note'.Theme
      }

      match DML.upsertNote env update with
      | Ok note ->
        log.Debug("Note {NoteId} updated.", note'.Id)
        if note.Theme <> note'.Theme then
          me.Classes.Remove(string note'.Theme) |> ignore
          me.Classes.Add(string note.Theme)
        note' <- note
        me.Title <- extractTitle note'.Content

      | Error failure ->
        log.Error(failure, "Failed to update Note {NoteId}.", note'.Id)
        MessageBox.Alert(failure.Message, owner=me)

    member me.Delete() =
      if MessageBox.Confirm("This cannot be undone. Continue?", owner=me) then
        match DML.deleteNote env note'.Id with
        | Ok () ->
          me.Close()

        | Error failure ->
          log.Error(failure, "Failed to delete Note {NoteId}.", note'.Id)
          MessageBox.Alert(failure.Message, owner=me)
