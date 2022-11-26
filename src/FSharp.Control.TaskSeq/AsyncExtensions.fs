namespace FSharp.Control

open FSharp.Control.TaskSeqBuilders

[<AutoOpen>]
module AsyncExtensions =

    // Add asynchronous for loop to the 'async' computation builder
    type Microsoft.FSharp.Control.AsyncBuilder with

        member _.For(source: taskSeq<'T>, action: 'T -> Async<unit>) =
            source
            |> TaskSeq.iterAsync (action >> Async.StartAsTask)
            |> Async.AwaitTask
