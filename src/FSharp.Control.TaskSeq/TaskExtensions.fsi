namespace FSharp.Control

#nowarn "1204"

[<AutoOpen>]
module TaskExtensions =

    type TaskBuilder with

        /// <summary>
        /// Inside <see cref="task" />, iterate over all values of a <see cref="taskSeq" />.
        /// </summary>
        member inline For: source: TaskSeq<'T> * body: ('T -> TaskCode<'TOverall, unit>) -> TaskCode<'TOverall, unit>
