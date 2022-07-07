namespace MulberryLabs.Genoteerd

open System
open System.Data
open System.IO
open System.Threading.Tasks
open MessageBox.Avalonia
open MessageBox.Avalonia.Enums

open type MessageBoxManager

/// Utilities for working with dialogs in AvaloniaUI
type MessageBox =
  /// Display a message to the end-user in a model dialog.
  static member Alert(prompt, ?buttons, ?title, ?owner) =
    let title' = defaultArg title "Genoteerd"
    let buttons' = defaultArg buttons ButtonEnum.Ok
    let dialog = GetMessageBoxStandardWindow(title', prompt, buttons')
    let task =
      match owner with
      | Some wo -> dialog.ShowDialog(wo)
      | None    -> dialog.Show()
    task.ContinueWith(Action<Task<_>>(ignore))


/// Contains miscellaneous utilities (n.b. this module is "auto-open'ed").
[<AutoOpen>]
module Library =
  /// Raises a System.InvalidProgram exception with the given message.
  let inline invalidProg (message : string) : 'T =
    raise (InvalidProgramException message)

  /// Returns the given value with all leading and trailing whitespace removed.
  let inline (|Trimmed|) (value: string) : string =
    if String.IsNullOrEmpty value then value else value.Trim()

  /// Reports the number of characters in the given value (n.b. reports zero
  /// for strings which are null, empty, or consist solely of whitespace).
  let inline (|Length|) (value: string) : uint32 =
    match value with
    | Trimmed null -> 0u
    | Trimmed item -> uint32 (String.length item)

  type IDbConnection with
    /// Initiates a new database transaction
    /// (while performing the necessary book-keeping).
    member me.StartTransaction() =
      try
        if me.State = ConnectionState.Closed then me.Open()
        me.BeginTransaction()
      with
      | _ -> reraise ()

  type DirectoryInfo with
    /// Returns a new string with the given paths appended, with the correct
    /// OS-specific path separator, to the full path of the current directory.
    member me.AppendPath([<ParamArray>] paths : string array) =
      Path.Combine [| me.FullName; yield! paths |]
