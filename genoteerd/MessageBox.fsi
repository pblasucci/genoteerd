namespace MulberryLabs.Genoteerd

open Avalonia.Controls


/// Utilities for working with dialogs in AvaloniaUI
[<Sealed>]
type MessageBox =
  /// Display a message to the end-user in a model dialog.
  /// string * string option * 'a option -> unit
  static member Alert :
    message : string * ?title : string * ?owner : Window -> unit

  /// Ask the end-user to make an 'yes or no' decision.
  static member Confirm :
    prompt : string * ?title : string * ?owner : Window -> bool
