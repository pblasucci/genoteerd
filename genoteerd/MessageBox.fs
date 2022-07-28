namespace MulberryLabs.Genoteerd

open System.Threading
open System.Threading.Tasks
open Avalonia.Controls
open Avalonia.Threading
open MessageBox.Avalonia
open MessageBox.Avalonia.DTO
open MessageBox.Avalonia.Enums

open type MessageBoxManager


module MsgBox =
  let showDialog (task' : unit -> Task<'T>) =
     let result = ref Unchecked.defaultof<'T>
     use cts = new CancellationTokenSource()
     let task = task' ()
     task.ContinueWith(
       (fun (t : Task<_>) ->
         result.Value <- t.Result
         cts.Cancel()
       ),
       TaskScheduler.FromCurrentSynchronizationContext()
     )
     |> ignore
     Dispatcher.UIThread.MainLoop(cts.Token)
     result.Value

  let makeDialog buttons title message =
    GetMessageBoxStandardWindow(
     MessageBoxStandardParams(
       ContentTitle = title,
       ContentMessage = message,
       ButtonDefinitions = buttons,
       MinWidth = 200.,
       MinHeight = 120.
     )
   )

  let makeAlert = makeDialog ButtonEnum.Ok
  let makeConfirm = makeDialog ButtonEnum.YesNo


[<Sealed>]
type MessageBox =
  static member Alert(message, ?title, ?owner : Window) =
    MsgBox.showDialog (fun () -> task {
      let dialog = MsgBox.makeAlert (defaultArg title "Genoteerd") message
      let task =
         match owner with
         | Some wo -> dialog.ShowDialog(wo)
         | None    -> dialog.Show()
      let! _ = task
      return ()
    })

  static member Confirm(prompt, ?title, ?owner : Window) =
    MsgBox.showDialog (fun () -> task {
      let dialog = MsgBox.makeConfirm (defaultArg title "Genoteerd") prompt
      let task =
         match owner with
         | Some wo -> dialog.ShowDialog(wo)
         | None    -> dialog.Show()
      let! choice = task
      return choice = ButtonResult.Yes
    })
