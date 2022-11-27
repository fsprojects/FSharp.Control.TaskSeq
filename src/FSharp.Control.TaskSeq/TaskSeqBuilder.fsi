namespace FSharp.Control

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Sources
open System.Runtime.CompilerServices
open System.Collections.Generic

open FSharp.Core.CompilerServices

[<AutoOpen>]
module Internal =

    /// Setting from environment variable TASKSEQ_LOG_VERBOSE, which,
    /// when set, enables (very) verbose printing of flow and state
    val initVerbose: unit -> bool

    /// Call MoveNext on an IAsyncStateMachine by reference
    val inline moveNextRef: x: byref<#IAsyncStateMachine> -> unit

    val inline raiseNotImpl: unit -> 'a

type taskSeq<'T> = IAsyncEnumerable<'T>

[<Class; NoComparison; NoEquality>]
type TaskSeqStateMachineData<'T> =

    new: unit -> TaskSeqStateMachineData<'T>

    [<DefaultValue(false)>]
    val mutable cancellationToken: CancellationToken

    /// Keeps track of the objects that need to be disposed off on IAsyncDispose.
    [<DefaultValue(false)>]
    val mutable disposalStack: ResizeArray<(unit -> Task)>

    [<DefaultValue(false)>]
    val mutable awaiter: ICriticalNotifyCompletion

    [<DefaultValue(false)>]
    val mutable promiseOfValueOrEnd: ManualResetValueTaskSourceCore<bool>

    /// Helper struct providing methods for awaiting 'next' in async iteration scenarios.
    [<DefaultValue(false)>]
    val mutable builder: AsyncIteratorMethodBuilder

    /// Whether or not a full iteration through the IAsyncEnumerator has completed
    [<DefaultValue(false)>]
    val mutable completed: bool

    /// Used by the AsyncEnumerator interface to return the Current value when
    /// IAsyncEnumerator.Current is called
    [<DefaultValue(false)>]
    val mutable current: ValueOption<'T>

    /// A reference to 'self', because otherwise we can't use byref in the resumable code.
    [<DefaultValue(false)>]
    val mutable boxedSelf: TaskSeq<'T>

    member PopDispose: unit -> unit

    member PushDispose: disposer: (unit -> Task) -> unit

and [<AbstractClass; NoEquality; NoComparison>] TaskSeq<'T> =
    interface IValueTaskSource<bool>
    interface IValueTaskSource
    interface IAsyncStateMachine
    interface IAsyncEnumerable<'T>
    interface IAsyncEnumerator<'T>

    new: unit -> TaskSeq<'T>

    abstract MoveNextAsyncResult: unit -> ValueTask<bool>

and [<NoComparison; NoEquality>] TaskSeq<'Machine, 'T
    when 'Machine :> IAsyncStateMachine and 'Machine :> IResumableStateMachine<TaskSeqStateMachineData<'T>>> =
    inherit TaskSeq<'T>
    interface IAsyncEnumerator<'T>
    interface IAsyncEnumerable<'T>
    interface IAsyncStateMachine
    interface IValueTaskSource<bool>
    interface IValueTaskSource

    new: unit -> TaskSeq<'Machine, 'T>

    [<DefaultValue(false)>]
    val mutable _initialMachine: 'Machine

    /// Keeps the active state machine.
    [<DefaultValue(false)>]
    val mutable _machine: 'Machine

    //new: unit -> TaskSeq<'Machine, 'T>
    member InitMachineData: ct: CancellationToken * machine: byref<'Machine> -> unit
    override MoveNextAsyncResult: unit -> ValueTask<bool>

and TaskSeqCode<'T> = ResumableCode<TaskSeqStateMachineData<'T>, unit>
and TaskSeqStateMachine<'T> = ResumableStateMachine<TaskSeqStateMachineData<'T>>

[<Class>]
type TaskSeqBuilder =

    member inline Combine: task1: TaskSeqCode<'T> * task2: TaskSeqCode<'T> -> TaskSeqCode<'T>
    member inline Delay: f: (unit -> TaskSeqCode<'T>) -> TaskSeqCode<'T>
    member inline Run: code: TaskSeqCode<'T> -> IAsyncEnumerable<'T>
    member inline TryFinally: body: TaskSeqCode<'T> * compensation: (unit -> unit) -> TaskSeqCode<'T>
    member inline TryFinallyAsync: body: TaskSeqCode<'T> * compensation: (unit -> Task) -> TaskSeqCode<'T>
    member inline TryWith: body: TaskSeqCode<'T> * catch: (exn -> TaskSeqCode<'T>) -> TaskSeqCode<'T>
    member inline Using: disp: 'a * body: ('a -> TaskSeqCode<'T>) -> TaskSeqCode<'T> when 'a :> IAsyncDisposable
    member inline While: condition: (unit -> bool) * body: TaskSeqCode<'T> -> TaskSeqCode<'T>
    /// Used by `For`. F# currently doesn't support `while!`, so this cannot be called directly from the CE
    member inline WhileAsync: condition: (unit -> ValueTask<bool>) * body: TaskSeqCode<'T> -> TaskSeqCode<'T>
    member inline Yield: v: 'T -> TaskSeqCode<'T>
    member inline Zero: unit -> TaskSeqCode<'T>

[<AutoOpen>]
module TaskSeqBuilder =

    /// <summary>
    /// Builds an asynchronous task sequence based on <see cref="IAsyncEnumerable&lt;'T&gt;" /> using computation expression syntax.
    /// </summary>
    val taskSeq: TaskSeqBuilder

[<AutoOpen>]
module LowPriority =
    type TaskSeqBuilder with

        [<NoEagerConstraintApplication>]
        member inline Bind< ^TaskLike, 'TResult1, 'TResult2, ^Awaiter, 'TOverall> :
            task: ^TaskLike * continuation: ('TResult1 -> TaskSeqCode<'TResult2>) -> TaskSeqCode<'TResult2>
                when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
                and ^Awaiter :> ICriticalNotifyCompletion
                and ^Awaiter: (member get_IsCompleted: unit -> bool)
                and ^Awaiter: (member GetResult: unit -> 'TResult1)

[<AutoOpen>]
module MediumPriority =
    type TaskSeqBuilder with

        member inline Using: disp: 'a * body: ('a -> TaskSeqCode<'T>) -> TaskSeqCode<'T> when 'a :> IDisposable

    type TaskSeqBuilder with

        member inline For: sequence: seq<'TElement> * body: ('TElement -> TaskSeqCode<'T>) -> TaskSeqCode<'T>

    type TaskSeqBuilder with

        member inline YieldFrom: source: seq<'T> -> TaskSeqCode<'T>

    type TaskSeqBuilder with

        member inline For:
            source: #IAsyncEnumerable<'TElement> * body: ('TElement -> TaskSeqCode<'T>) -> TaskSeqCode<'T>

    type TaskSeqBuilder with

        member inline YieldFrom: source: IAsyncEnumerable<'T> -> TaskSeqCode<'T>

[<AutoOpen>]
module HighPriority =
    type TaskSeqBuilder with

        member inline Bind: task: Task<'TResult1> * continuation: ('TResult1 -> TaskSeqCode<'T>) -> TaskSeqCode<'T>
