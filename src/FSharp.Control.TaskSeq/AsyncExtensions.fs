namespace FSharp.Control

[<AutoOpen>]
module AsyncExtensions =

    // Add asynchronous for loop to the 'async' computation builder
    type Microsoft.FSharp.Control.AsyncBuilder with

        member _.For(source: TaskSeq<'T>, action: 'T -> Async<unit>) =
            source
            |> TaskSeq.iterAsync (action >> Async.StartImmediateAsTask)
            |> Async.AwaitTask
