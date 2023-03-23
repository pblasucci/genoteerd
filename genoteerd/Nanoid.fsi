namespace MulberryLabs.Genoteerd

/// An abbreviation for the CLI type System.String.
[<MeasureAnnotatedAbbreviation>]
type string<[<Measure>] 'Measure> = string


/// Can be attached to a string to better indicate its purpose.
[<Measure>]
type nanoid =
  /// Used to "tag" a string as a NanoId
  static member tag : value : string -> string<nanoid>


/// Contains utilities for working with NanoIds.
[<Sealed>]
[<AbstractClass>]
[<RequireQualifiedAccess>]
type NanoId =
  /// Generates a random 21 character string using the alphabet
  /// "_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".
  static member NewId : unit -> string<nanoid>
