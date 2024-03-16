namespace FSharp.Control

#nowarn "1204" // This construct is for use by compiled F# code and should not be used directly.

[<AutoOpen>]
module TaskExtensions =

    type TaskBuilder with

        /// <summary>
        /// Inside <see cref="task" />, iterate over all values of a <see cref="taskSeq" />.
        /// </summary>
        member inline For: source: TaskSeq<'T> * body: ('T -> TaskCode<'TOverall, unit>) -> TaskCode<'TOverall, unit>
