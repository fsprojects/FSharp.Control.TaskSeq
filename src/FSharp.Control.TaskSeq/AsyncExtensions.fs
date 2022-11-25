namespace FSharp.Control

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

#nowarn "57"
#nowarn "1204"
#nowarn "3513"


[<AutoOpen>]
module AsyncExtensions =

    // Add asynchronous for loop to the 'async' computation builder
    type Microsoft.FSharp.Control.AsyncBuilder with

        member x.For(tasksq: IAsyncEnumerable<'T>, action: 'T -> Async<unit>) =
            tasksq
            |> TaskSeq.iterAsync (action >> Async.StartAsTask)
            |> Async.AwaitTask


    // temp example
    let foo () = async {
        let mutable sum = 0

        let xs = taskSeq {
            1
            2
            3
        }

        for x in xs do
            sum <- sum + x
    }
