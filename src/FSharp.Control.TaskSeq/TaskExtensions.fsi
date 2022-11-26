namespace FSharp.Control

#nowarn "1204"

[<AutoOpen>]
module TaskExtensions =
    open FSharp.Control.TaskSeqBuilders

    type TaskBuilder with

        /// <summary>
        /// Inside <see cref="task" />, iterate over all values of a <see cref="taskSeq" />.
        /// </summary>
        member inline For: source: taskSeq<'T> * body: ('T -> TaskCode<'TOverall, unit>) -> TaskCode<'TOverall, unit>
