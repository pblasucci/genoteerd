namespace MulberryLabs.Genoteerd

#nowarn "3388" // ⮜⮜⮜ Control DSL relies heavily on subsumption.

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Media

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
      | otherwise -> failwith $"Unknown Dock enumeration value: {otherwise}"
    ]

  let resizeGrips : IView list = [
    edgeGrip Dock.Top
    edgeGrip Dock.Left
    edgeGrip Dock.Bottom
    edgeGrip Dock.Right
  ]

  let header (host : IStickyNoteHost) : IView list = [
    (* header bar with buttons *)
    Button.create [
      Button.column 0
      Button.row 0
      Button.content "✚"
      Button.onClick (fun e ->
        e.Handled <- true
        host.Launch()
      )
    ]
    WrapPanel.create [ WrapPanel.column 1; WrapPanel.row 0 ]
    Button.create [
      Button.column 2
      Button.row 0
      Button.content "✖"
      Button.onClick (fun e ->
        e.Handled <- true
        host.Delete()
      )
    ]
  ]

  let footer (_ : IStickyNoteHost) : IView list = [
    Button.create [
      Button.column 2
      Button.row 2
      Button.content "⚙"
      Button.isEnabled false
    ]
    //TODO handle themes popup
  ]

  let content (host : IStickyNoteHost) (state : IWritable<string>) =
    DockPanel.create [
      (* edges for resizing *)
      DockPanel.children [
        yield! resizeGrips
        (* main content *)
        Grid.create [
          Grid.columnDefinitions "Auto, *, Auto"
          Grid.rowDefinitions "Auto, *, Auto"
          Grid.children [
            yield! header host
            (* actual editable area! *)
            TextBox.create [
              TextBox.column 0
              TextBox.row 1
              TextBox.columnSpan 3
              TextBox.acceptsReturn true
              TextBox.textWrapping TextWrapping.Wrap
              TextBox.text state.Current
              TextBox.onTextChanged state.Set
            ]
            yield! footer host
          ]
        ]
      ]
    ]

  let main (host : IStickyNoteHost) (note : string) =
    Component(fun context ->
      let state = context.useState note

      context.useEffect (
        handler = (fun () -> host.Upsert(state.Current)),
        triggers = [ EffectTrigger.AfterChange state ]
      )

      content host state
    )
