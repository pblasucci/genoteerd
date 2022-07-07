namespace MulberryLabs.Genoteerd

#nowarn "42" (* inline IL used in this file (see line ?) *)

/// An abbreviation for the CLI type System.String.
[<MeasureAnnotatedAbbreviation>] type string<[<Measure>] 'Measure> = string


/// Provides definitions of the Taggable "trait" for well-known types.
type Taggable =
  static member Tag(_ : string<'Source>, _ : string<'Target>) = ()
  static member Tag(_ : string<'Source> array, _ : string<'Target> array) = ()


module [<AutoOpen>] Operators =
  let inline private ``¡tag!``<
    ^P, ^S, ^T when (^P or ^T) : (static member Tag : ^S * ^T -> unit)
  > (value : ^S) = (# "" value : ^T #)

  let inline private ``¡untag!``<
    ^P, ^S, ^T when (^P or ^S) : (static member Tag : ^S * ^T -> unit)
  > (value : ^S) = (# "" value : ^T #)

  /// Applies a general-purpose "tag" to a given value
  /// (n.b. the return value type MUST support the Taggable "trait").
  let inline tag (value : 'T) : 'Tagged =
    ``¡tag!``<Taggable, 'T, 'Tagged> value

  /// Removes a general-purpose "tag" from a given value
  /// (n.b. the input value type MUST support the Taggable "trait").
  let inline untag (value : 'Tagged) : 'T =
    ``¡untag!``<Taggable, 'Tagged, 'T> value
