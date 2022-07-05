namespace MulberryLabs.Genoteerd

open System
open System.Data
open System.IO
open MessageBox.Avalonia
open MessageBox.Avalonia.Enums

open type MessageBoxManager

type MessageBox =
  static member Alert(prompt, ?title, ?buttons) =
    let title' = defaultArg title "Genoteerd"
    let buttons' = defaultArg buttons ButtonEnum.Ok
    let dialog = GetMessageBoxStandardWindow(title', prompt, buttons')
    dialog.Show() |> ignore


/// Contains miscellaneous utilities (n.b. this module is "auto-open'ed").
[<AutoOpen>]
module Library =
  let inline invalidProgram (message : string) : 'T =
    raise (InvalidProgramException message)

  let (|Trimmed|) (value: string) : string =
    if String.IsNullOrEmpty value then value else value.Trim()

  let (|Length|) (value: string) : uint32 =
    match value with
    | Trimmed null -> 0u
    | Trimmed item -> uint32 (String.length item)


  type IDbConnection with
    /// Initiates a new database transaction
    /// (while performing the necessary bookkeeping).
    member me.StartTransaction() =
      try
        if me.State = ConnectionState.Closed then me.Open()
        me.BeginTransaction()
      with
      | _ -> reraise ()


  type DirectoryInfo with
    member me.AppendPath([<ParamArray>] paths : string array) =
      Path.Combine [| me.FullName; yield! paths |]
