namespace MulberryLabs.Genoteerd

#nowarn "3388" // ⮜⮜⮜ Control DSL relies heavily on subsumption.

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Media

open type Brushes


[<RequireQualifiedAccess>]
module StickyNoteView =
  let edgeGrip dock =
    WrapPanel.create [
      WrapPanel.dock dock
      match dock with
      | Dock.Top
      | Dock.Bottom ->
        WrapPanel.cursor (Cursor.Parse "SizeNorthSouth")
        WrapPanel.height 2.
        WrapPanel.classes [ "edge"; "NS" ]
      | Dock.Left
      | Dock.Right ->
        WrapPanel.cursor (Cursor.Parse "SizeWestEast")
        WrapPanel.width 2.
        WrapPanel.classes [ "edge"; "WE" ]
      | otherwise ->
        failwith $"Unknown Dock enumeration value: {otherwise}"
    ]
    :> IView

  let resizeGrips = [
    edgeGrip Dock.Top
    edgeGrip Dock.Left
    edgeGrip Dock.Bottom
    edgeGrip Dock.Right
  ]

  let themesMenu (host : IStickyNoteHost) (state : IWritable<string>) =
    MenuItem.create [
      MenuItem.header "Themes"
      MenuItem.viewItems [
        for theme in NoteTheme.AllThemes do
          MenuItem.create [
            MenuItem.header theme.Humanized
            MenuItem.classes [string theme]
            MenuItem.onClick (fun e ->
              e.Handled <- true
              host.Upsert(state.Current, theme)
            )
          ]
      ]
    ]

  let headerMenu (host : IStickyNoteHost) (state : IWritable<string>) =
    ContextMenu.create [
      ContextMenu.viewItems [
        themesMenu host state
        Separator.create []
        MenuItem.create [
          MenuItem.header "Quit"
          MenuItem.onClick (fun e -> e.Handled <- true; host.Quit())
        ]
      ]
    ]

  let header
    (host : IStickyNoteHost)
    (state : IWritable<string>)
    : IView list
    = [
      (* header bar with buttons *)
      Button.create [
        Button.column 0
        Button.row 0
        Button.content "✚"
        Button.tip "New note"
        Button.onClick (fun e -> e.Handled <- true; host.Launch())
      ]
      WrapPanel.create [
        WrapPanel.column 1
        WrapPanel.row 0
        WrapPanel.classes [ "header" ]
        WrapPanel.tip "Left-click to drag. Right-click for options."
        WrapPanel.contextMenu (headerMenu host state)
      ]
      Button.create [
        Button.column 2
        Button.row 0
        Button.content "✖"
        Button.tip "Delete note"
        Button.onClick (fun e -> e.Handled <- true; host.Delete())
      ]
    ]

  let content (host : IStickyNoteHost) (state : IWritable<string>) =
    DockPanel.create [
      (* edges for resizing *)
      DockPanel.children [
        yield! resizeGrips
        (* main content *)
        Grid.create [
          Grid.columnDefinitions "Auto, *, Auto"
          Grid.rowDefinitions "Auto, *"
          Grid.children [
            yield! header host state
            (* actual editable area! *)
            TextBox.create [
              TextBox.name "note"
              TextBox.column 0
              TextBox.row 1
              TextBox.columnSpan 3
              TextBox.acceptsReturn true
              TextBox.textWrapping TextWrapping.Wrap
              TextBox.text state.Current
              TextBox.onTextChanged state.Set
            ]
          ]
        ]
      ]
    ]

  let main (host : IStickyNoteHost) text =
    Component(fun context ->
      let state = context.useState text

      context.useEffect (
        handler = (fun () -> host.Upsert(state.Current)),
        triggers = [ EffectTrigger.AfterChange state ]
      )

      content host state
    )
