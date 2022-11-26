namespace FSharp.Control

[<AutoOpen>]
module AsyncExtensions =
    open FSharp.Control.TaskSeqBuilders

    type AsyncBuilder with

        /// Iterate over all values of a taskSeq.
        member For: source: taskSeq<'T> * action: ('T -> Async<unit>) -> Async<unit>
