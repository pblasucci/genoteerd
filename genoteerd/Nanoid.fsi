namespace MulberryLabs.Genoteerd

type [<Measure>] nanoid

/// Contains utilities for working with NanoIds
[<RequireQualifiedAccess>]
type [<Sealed; AbstractClass>] NanoId =
  /// Generates a random 21 character string using the alphabet
  /// "_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
  static member NewId : unit -> string<nanoid>
