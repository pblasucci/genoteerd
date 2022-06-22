namespace MulberryLabs.Genoteerd

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Hosts
open Avalonia.Platform

open MulberryLabs.Genoteerd
open MulberryLabs.Genoteerd.Storage

type StickyNoteHost(env: AppEnv, ?note : Note) as me =
  inherit HostWindow()
  let host = me :> IStickyNoteHost
  let (Trace log) = env

  let mutable note' = note |> Option.defaultWith Note.New

  do (* .ctor *)
    let top, right, bottom, left =
      match note with
      | None ->
        (None, None, None, None)
      | Some n ->
        let geo = n.Geometry
        (Some geo.Y, Some geo.Width, Some geo.Height, Some geo.X)

    me.SystemDecorations <- SystemDecorations.None
    me.ExtendClientAreaChromeHints <- ExtendClientAreaChromeHints.NoChrome
    me.WindowStartupLocation <- WindowStartupLocation.Manual
    me.ShowInTaskbar <- true
    me.MinHeight <- 120.
    me.MinWidth <- 120.
    me.Width <- defaultArg right 360.
    me.Height <- defaultArg bottom 360.
    me.Title <- "Genoteerd"
    me.Content <- StickyNoteView.main host note'.Content

    me.Opened.Add(fun _ ->
      let pos = me.Position
      me.Position <-
        match left, top with
        | Some x, Some y  -> PixelPoint(int x, int y)
        | None  , Some y  -> PixelPoint(pos.X, int y)
        | Some x, None    -> PixelPoint(int x, pos.Y)
        | None  , None    -> pos
    )

    // geometric event handling
    me.PointerPressed.Add(fun args ->
      args.Handled <- true
      // resizing
      match args.Source with
      | :? WrapPanel as edge when edge.Classes.Contains "edge" ->
        let windowEdge =
          match DockPanel.GetDock(edge) with
          | Dock.Top -> WindowEdge.North
          | Dock.Right -> WindowEdge.East
          | Dock.Bottom -> WindowEdge.South
          | Dock.Left -> WindowEdge.West
          | otherwise -> failwith $"Unknown Dock enumeration value: {otherwise}"
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
    member _.Launch() =
      let host = StickyNoteHost(env)
      //TODO change the args to be relative to location of current note
      //TODO ensure new note is never opened off-screen
      host.Show()

    member me.Upsert(content) =
      let origin = me.Position
      let result =
        {
          note' with
            Content = content
            Geometry = Rect(origin.X, origin.Y, me.Width, me.Height)
        }
        |> DML.upsertNote env

      match result with
      | Ok note ->
          log.Debug("Note {NoteId} updated.", note'.Id)
          note' <- note
      | Error failure ->
          log.Error(failure, "Failed to update Note {NoteId}.", note'.Id)
          MessageBox.Alert(failure.Message)

    member me.Delete() =
      match DML.deleteNote env note'.Id with
      | Ok () -> me.Close()
      | Error failure ->
          log.Error(failure, "Failed to delete Note {NoteId}.", note'.Id)
          MessageBox.Alert(failure.Message)
