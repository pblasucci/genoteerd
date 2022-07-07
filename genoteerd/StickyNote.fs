namespace MulberryLabs.Genoteerd

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

    /// Currently not supported!
    Theme : NoteTheme

    /// The point-in-time when the note was last persisted (or never).
    UpdatedAt : ZonedDateTime option
  }
  /// Returns `true` when the note has never been persisted; false otherwise.
  member me.IsNew = Option.isNone me.UpdatedAt

  /// Creates a new empty note, at its minimum
  /// size, in the default on-screen location.
  static member New() =
    {
      Id = NanoId.NewId()
      Content = ""
      Geometry = Rect(0., 0., 120., 120.)
      Theme = Default
      UpdatedAt = None
    }

/// Currently not supported!
and NoteTheme =
  | BlueMonday
  | Briquette
  | Classic
  | Default
  | PinkyPie
  | Redemption
  | RitesOfSpring
  | Whitesmoke


/// Defines some basic functionality a "sticky note" needs,
/// but with which a "sticky note" isn't actually concerned.
type IStickyNoteHost =
  /// Spawns a new empty note, at its minimum
  /// size, in the default on-screen location.
  abstract Launch : unit -> unit

  /// Discards a note from persistent storage.
  abstract Delete : unit -> unit

  /// Creates a note in persistent storage,
  /// or updates an existing note in storage.
  abstract Upsert : text : string -> unit
