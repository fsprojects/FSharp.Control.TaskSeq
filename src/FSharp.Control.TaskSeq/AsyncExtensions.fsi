namespace FSharp.Control

[<AutoOpen>]
module AsyncExtensions =
    open FSharp.Control.TaskSeqBuilders

    type AsyncBuilder with

        /// <summary>
        /// Inside <see cref="async" />, iterate over all values of a <see cref="taskSeq" />.
        /// </summary>
        member For: source: taskSeq<'T> * action: ('T -> Async<unit>) -> Async<unit>
