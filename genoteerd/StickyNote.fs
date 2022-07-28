namespace MulberryLabs.Genoteerd

open System.Text.RegularExpressions
open System.Windows.Input
open FSharp.Reflection
open Avalonia
open NodaTime


/// Logical definition of a single "sticky note" (basically, a suped-up db row).
[<NoComparison>]
type Note =
  {
    /// Uniquely identifies a note (mostly for storage purposes).
    Id : string<nanoid>

    /// The actual text of the note (n.b. empty notes are NOT persisted).
    Content : string

    /// The on-screen coordinates of the note.
    Geometry : Rect

    /// Determines the styling applied to an individual note.
    Theme : NoteTheme

    /// The point-in-time when the note was last persisted (or never).
    UpdatedAt : ZonedDateTime option
  }
  /// Returns `true` when the note has never been persisted; false otherwise.
  member me.IsNew = Option.isNone me.UpdatedAt

  /// Creates a new empty note, at its minimum
  /// size, in the default on-screen location.
  static member New(?theme) =
    {
      Id = NanoId.NewId()
      Content = ""
      Geometry = Rect(0., 0., 120., 120.)
      Theme = defaultArg theme Default
      UpdatedAt = None
    }

/// Identifies the styling applied to a note.
and NoteTheme =
  | BlueMonday
  | Briquette
  | Classic
  | Default
  | PinkyPie
  | Redemption
  | RitesOfSpring
  | Whitesmoke

[<RequireQualifiedAccess>]
module private ThemeMap =
  let lookup =
    typeof<NoteTheme>
    |> FSharpType.GetUnionCases
    |> Array.map (fun case ->
      let item = FSharpValue.MakeUnion(case, null)
      (case.Name, unbox<NoteTheme> item)
    )
    |> Map.ofSeq

type NoteTheme with
  /// A 'display friendly' stringification of the note theme.
  member me.Humanized = Regex.Replace(string me, "[A-Z]", " $0").Trim()

  /// A collection of all possible themes for a note.
  static member AllThemes = ThemeMap.lookup.Values
  /// Tries to create a note theme from its stringified equivalent
  static member TryParse(raw) = ThemeMap.lookup.TryFind(raw)


/// Defines some basic functionality a "sticky note" needs,
/// but with which a "sticky note" isn't actually concerned.
type IStickyNoteHost =
  /// Performs a graceful shutdown of the application.
  abstract Quit : unit -> unit

  /// Spawns a new empty note, at its minimum
  /// size, in the default on-screen location.
  abstract Launch : unit -> unit

  /// Discards a note from persistent storage.
  abstract Delete : unit -> unit

  /// Creates a note in persistent storage,
  /// or updates an existing note in storage.
  abstract Upsert : text : string * ?theme : NoteTheme -> unit
