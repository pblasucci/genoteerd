module MulberryLabs.Genoteerd.Storage

/// Functions for working with the database schema.
[<RequireQualifiedAccess>]
module DDL =
  /// Establishes the correct schema inside a database
  /// (n.b. existing schemata will be discarded).
  val install : env : IStore -> Result<unit, exn>


/// Functions for performing mutative database actions (create, update, delete).
[<RequireQualifiedAccess>]
module DML =
  /// Create a new entry in the database, unless a row with the given noteId
  /// already exists, in which case the row data is simply updated.
  val upsertNote : env : 'T -> note : Note -> Result<Note, exn>
    when 'T :> IClock and 'T :> IStore

  /// Drops a row with the given noteID from the database
  /// (n.b. invalid noteId causes this to be a non-operation).
  val deleteNote : env : IStore -> noteId : string<nanoid> -> Result<unit, exn>


/// Functions for querying data from the database.
[<RequireQualifiedAccess>]
module SQL =
  /// Gathers all notes currently stored in the database.
  val selectAllNotes : env : IStore -> Result<Note list, exn>
