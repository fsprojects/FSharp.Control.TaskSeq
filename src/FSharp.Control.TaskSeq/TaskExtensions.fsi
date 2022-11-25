namespace FSharp.Control

#nowarn "1204"

[<AutoOpen>]
module TaskExtensions =

    val WhileDynamic:
        sm: byref<TaskStateMachine<'Data>> *
        condition: (unit -> System.Threading.Tasks.ValueTask<bool>) *
        body: TaskCode<'Data, unit> ->
            bool

    val WhileBodyDynamicAux:
        sm: byref<TaskStateMachine<'Data>> *
        condition: (unit -> System.Threading.Tasks.ValueTask<bool>) *
        body: TaskCode<'Data, unit> *
        rf: TaskResumptionFunc<'Data> ->
            bool

    type TaskBuilder with

        member inline WhileAsync:
            condition: (unit -> System.Threading.Tasks.ValueTask<bool>) * body: TaskCode<'TOverall, unit> ->
                TaskCode<'TOverall, unit>

        member inline For:
            tasksq: System.Collections.Generic.IAsyncEnumerable<'T> * body: ('T -> TaskCode<'TOverall, unit>) ->
                TaskCode<'TOverall, unit>
