namespace MulberryLabs.Genoteerd

#nowarn "9" (* Unverifiable IL - see `stackspan` function for details *)

open System

type [<Measure>] nanoid

module NanoId =
  open System.Security.Cryptography
  open FSharp.NativeInterop

  open type System.Numerics.BitOperations

  let [<Literal>] Size = 21
  let [<Literal>] Alphabet =
    "_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"

  let inline (|Length|) value =
    Length(if String.IsNullOrWhiteSpace value then 0 else value.Trim().Length)

  let inline stackspan<'T when 'T : unmanaged> size =
    Span<'T>(size |> NativePtr.stackalloc<'T> |> NativePtr.toVoidPtr, size)

  let inline makeNewId (Length length as alphabet) =
    let mask = (2 <<< 31 - LeadingZeroCount((uint32 length - 1u) ||| 1u)) - 1
    let step = int (ceil (1.6 * float mask * float Size / float length))

    let nanoid = stackspan<char> Size
    let buffer = stackspan<byte> step

    let mutable nanoidCount = 0
    let mutable bufferCount = 0
    let mutable runNextLoop = true

    while runNextLoop do
      RandomNumberGenerator.Fill(buffer)

      while runNextLoop do
        let index = int buffer[bufferCount] &&& mask
        if index < length then
          nanoid[nanoidCount] <- alphabet[index]

        bufferCount <- bufferCount + 1
        nanoidCount <- nanoidCount + 1
        runNextLoop <- nanoidCount < Size

    String(nanoid.ToArray())

open NanoId

[<RequireQualifiedAccess>]
type [<Sealed; AbstractClass>] NanoId =
  static member NewId() : string<nanoid> = tag (makeNewId Alphabet)
