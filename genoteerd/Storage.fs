module MulberryLabs.Genoteerd.Storage

open System.Data
open Avalonia
open Dapper
open Microsoft.FSharp.Core
open NodaTime
open NodaTime.Text

open type DateTimeZoneProviders


let stamp = ZonedDateTimePattern.GeneralFormatOnlyIso.WithZoneProvider(Tzdb)


[<RequireQualifiedAccess>]
module DDL =
  [<Literal>]
  let Notes =
    """
    DROP TABLE IF EXISTS notes;
    CREATE TABLE IF NOT EXISTS notes(
        note_id TEXT PRIMARY KEY,     -- NANOID

        content     TEXT      NULL,
        theme       TEXT      NULL,
        pos_x       REAL  NOT NULL,
        pos_y       REAL  NOT NULL,
        height      REAL  NOT NULL,
        width       REAL  NOT NULL,
        updated_at  TEXT  NOT NULL,   -- ISO-8601 date and time with zone

        CHECK (120 <= height),
        CHECK (120 <= width)
    );
    """

  let install (Store store) =
    use db = store.Connect()
    use tx = db.StartTransaction()
    try
      db.Execute(sql = Notes, transaction = tx) |> ignore
      tx.Commit()
      Ok()
    with x ->
      tx.Rollback()
      Error x


[<RequireQualifiedAccess>]
module DML =
  [<Literal>]
  let SaveNote =
    """
    REPLACE INTO notes(
        note_id,
        content,
        theme,
        pos_x,
        pos_y,
        height,
        width,
        updated_at
    ) VALUES (
        @noteId,
        @content,
        @theme,
        @posX,
        @posY,
        @height,
        @width,
        @updatedAt
    );
    """

  [<Literal>]
  let DropNote =
    """
    DELETE FROM notes WHERE note_id = @noteId;
    """

  let upsertNote (Clock clock & Store store) (note : Note) =
    use db = store.Connect()
    use tx = db.StartTransaction()
    try
      let now : ZonedDateTime = clock.JustNow()
      db.Execute(
        sql = SaveNote,
        param = {|
          noteId = string note.Id
          content = note.Content
          theme = string note.Theme
          posX = note.Geometry.X
          posY = note.Geometry.Y
          height = note.Geometry.Height
          width = note.Geometry.Width
          updatedAt = stamp.Format(now)
        |},
        transaction = tx
      )
      |> ignore
      tx.Commit()
      Ok { note with UpdatedAt = Some now }
    with x ->
      tx.Rollback()
      Error x

  let deleteNote (Store store) (noteId : string<nanoid>) =
    use db = store.Connect()
    use tx = db.StartTransaction()
    try
      db.Execute(
        sql = DropNote,
        param = {| noteId = string noteId |},
        transaction = tx
      )
      |> ignore
      tx.Commit()
      Ok()
    with x ->
      tx.Rollback()
      Error x


[<RequireQualifiedAccess>]
module SQL =
  [<Literal>]
  let AllNotes =
    """
      SELECT
          note_id,
          content,
          theme,
          pos_x,
          pos_y,
          height,
          width,
          updated_at
      FROM notes
      ORDER BY updated_at DESC;
      """

  [<CLIMutable>]
  type note_row = {
    note_id : string
    content : string
    theme : string
    pos_x : float
    pos_y : float
    height : float
    width : float
    updated_at : string
  }

  let selectAllNotes (Store store) =
    use db : IDbConnection = store.Connect()
    try
      let rows = db.Query<note_row>(AllNotes)
      Ok [
        for row in rows do
          let stamp' = stamp.Parse(row.updated_at).GetValueOrThrow()
          {
            Id = nanoid.tag row.note_id
            Content = row.content
            Theme = defaultArg (NoteTheme.TryParse row.theme) Default
            Geometry = Rect(row.pos_x, row.pos_y, row.width, row.height)
            UpdatedAt = Some stamp'
          }
      ]
    with x ->
      Error x
