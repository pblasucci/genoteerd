namespace MulberryLabs.Genoteerd

#nowarn "42" (* inline IL used in this file (see line ?) *)

/// An abbreviation for the CLI type System.Boolean.
[<MeasureAnnotatedAbbreviation>] type bool<[<Measure>] 'Measure> = bool

/// An abbreviation for the CLI type System.String.
[<MeasureAnnotatedAbbreviation>] type string<[<Measure>] 'Measure> = string


/// Provides definitions of the Taggable "trait" for well-known types.
type Taggable =
  static member Tag(_ : bool<'Source>, _ : bool<'Target>) = ()
  static member Tag(_ : string<'Source>, _ : string<'Target>) = ()


module [<AutoOpen>] Operators =
  let inline private ``¡tag!``<
    ^P, ^S, ^T when (^P or ^T) : (static member Tag : ^S * ^T -> unit)
  > (value : ^S) = (# "" value : ^T #)

  /// Applies a general-purpose "tag" to a given value
  /// (n.b. the target return type MUST support the Taggable "trait").
  let inline tag (value : 'T) : 'Tagged = ``¡tag!``<Taggable, 'T, 'Tagged> value
