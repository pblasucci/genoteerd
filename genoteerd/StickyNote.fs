namespace MulberryLabs.Genoteerd

open Avalonia
open NodaTime


[<NoComparison>]
type Note =
  {
    Id : string<nanoid>
    Content : string
    Geometry : Rect
    Theme : NoteTheme
    UpdatedAt : ZonedDateTime option
  }
  member me.IsNew = Option.isNone me.UpdatedAt
  static member New() =
    {
      Id = NanoId.NewId()
      Content = ""
      Geometry = Rect(0., 0., 120., 120.)
      Theme = Default
      UpdatedAt = None
    }

and NoteTheme =
  | BlueMonday
  | Briquette
  | Classic
  | Default
  | PinkyPie
  | Redemption
  | RitesOfSpring
  | Whitesmoke


type IStickyNoteHost =
  abstract Launch : unit -> unit
  abstract Delete : unit -> unit
  abstract Upsert : text : string -> unit
