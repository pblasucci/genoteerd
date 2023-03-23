namespace MulberryLabs.Genoteerd

#nowarn "9" (* Unverifiable IL - see `stackspan` function for details *)
#nowarn "42" (* inline IL used in this file (see `nanoid.tag` function for details) *)

open System


[<MeasureAnnotatedAbbreviation>]
type string<[<Measure>] 'Measure> = string


[<Measure>]
type nanoid =
  static member tag value = (# "" (value : string) : string<nanoid> #)


module NanoId =
  open System.Security.Cryptography
  open FSharp.NativeInterop

  open type System.Numerics.BitOperations

  [<Literal>]
  let Size = 21

  [<Literal>]
  let Range = "_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"

  let inline stackspan<'T when 'T : unmanaged> size =
    Span<'T>(size |> NativePtr.stackalloc<'T> |> NativePtr.toVoidPtr, size)

  let inline makeNewId (alphabet & Length length) =
    let mask = (2 <<< 31 - LeadingZeroCount((uint32 length - 1u) ||| 1u)) - 1
    let step = int (ceil (1.6 * float mask * float Size / float length))

    let nanoid = stackspan<char> Size
    let mutable nanoidCount = 0

    let buffer = stackspan<byte> step
    let mutable bufferCount = 0

    while nanoidCount < Size do
      RandomNumberGenerator.Fill(buffer)
      bufferCount <- 0

      while nanoidCount < Size && bufferCount < step do
        let index = int buffer[bufferCount] &&& mask
        bufferCount <- bufferCount + 1

        if index < int length then
          nanoid[nanoidCount] <- alphabet[index]
          nanoidCount <- nanoidCount + 1

    nanoid.ToString()


[<Sealed>]
[<AbstractClass>]
[<RequireQualifiedAccess>]
type NanoId =
  static member NewId() : string<nanoid> =
    nanoid.tag (NanoId.makeNewId NanoId.Range)
