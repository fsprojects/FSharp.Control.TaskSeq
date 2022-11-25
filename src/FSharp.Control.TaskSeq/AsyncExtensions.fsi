namespace FSharp.Control

#nowarn "1204"

[<AutoOpen>]
module AsyncExtensions =

    type AsyncBuilder with

        member For: tasksq: System.Collections.Generic.IAsyncEnumerable<'T> * action: ('T -> Async<unit>) -> Async<unit>
