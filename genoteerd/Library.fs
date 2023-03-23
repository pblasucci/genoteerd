namespace MulberryLabs.Genoteerd

open System
open System.Data
open System.IO
open Avalonia
open Avalonia.Controls.ApplicationLifetimes


/// Contains miscellaneous utilities (n.b. this module is "auto-open'ed").
[<AutoOpen>]
module Library =
  /// Raises a System.InvalidProgram exception with the given message.
  let inline invalidProg (message : string) : 'T =
    raise (InvalidProgramException message)

  /// Returns the given value with all leading and trailing whitespace removed
  /// (note: null values are coerced to an empty string).
  let inline (|Trimmed|) (value : string) : string =
    if String.IsNullOrEmpty value then "" else value.Trim()

  /// Reports the number of characters in the given value (n.b. reports zero
  /// for strings which are null, empty, or consist solely of whitespace).
  let inline (|Length|) (value : string) : uint32 =
    match value with
    | Trimmed null -> 0u
    | Trimmed item -> uint32 (String.length item)


  type IDbConnection with
    /// Initiates a new database transaction
    /// (while performing the necessary book-keeping).
    member me.StartTransaction() =
      try
        if me.State = ConnectionState.Closed then
          me.Open()
        me.BeginTransaction()
      with _ ->
        reraise ()


  type DirectoryInfo with
    /// Returns a new string with the given paths appended, with the correct
    /// OS-specific path separator, to the full path of the current directory.
    member me.AppendPath([<ParamArray>] paths : string array) =
      Path.Combine [| me.FullName; yield! paths |]


  type Application with
    /// Short-cut for access the IApplicationLifetime for desktop applications.
    static member CurrentDesktop =
      match Application.Current.ApplicationLifetime with
      | :? IClassicDesktopStyleApplicationLifetime as desktop -> Some desktop
      | _ -> None
