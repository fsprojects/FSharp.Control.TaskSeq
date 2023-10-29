namespace FSharp.Control

open System.Diagnostics

#nowarn "57" // note: this is *not* an experimental feature, but they forgot to switch off the flag

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices
open System.Threading.Tasks.Sources

open FSharp.Core.CompilerServices
open FSharp.Core.CompilerServices.StateMachineHelpers
open FSharp.Control


[<AutoOpen>]
module Internal = // cannot be marked with 'internal' scope

    let initVerbose () =
        try
            match Environment.GetEnvironmentVariable "TASKSEQ_LOG_VERBOSE" with
            | null -> false
            | x ->
                match x.ToLowerInvariant().Trim() with
                | "1"
                | "true"
                | "on"
                | "yes" -> true
                | _ -> false

        with _ ->
            false


    let inline moveNextRef (x: byref<'T> when 'T :> IAsyncStateMachine) = x.MoveNext()

    let inline raiseNotImpl () =
        NotImplementedException "Abstract Class: method or property not implemented"
        |> raise

type taskSeq<'T> = IAsyncEnumerable<'T>


[<NoComparison; NoEquality>]
type TaskSeqStateMachineData<'T>() =

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
    val mutable boxedSelf: TaskSeqBase<'T>

    member data.PushDispose(disposer: unit -> Task) =
        if isNull data.disposalStack then
            data.disposalStack <- ResizeArray()

        data.disposalStack.Add disposer

    member data.PopDispose() =
        if not (isNull data.disposalStack) then
            data.disposalStack.RemoveAt(data.disposalStack.Count - 1)

and [<AbstractClass; NoEquality; NoComparison>] TaskSeqBase<'T>() =

    abstract MoveNextAsyncResult: unit -> ValueTask<bool>

    interface IAsyncEnumerator<'T> with
        member _.Current = raiseNotImpl ()
        member _.MoveNextAsync() = raiseNotImpl ()
        member _.DisposeAsync() = raiseNotImpl ()

    interface IAsyncEnumerable<'T> with
        member _.GetAsyncEnumerator(ct) = raiseNotImpl ()

    interface IAsyncStateMachine with
        member _.MoveNext() = raiseNotImpl ()
        member _.SetStateMachine(_state) = raiseNotImpl ()

    interface IValueTaskSource with
        member _.GetResult(_token: int16) = raiseNotImpl ()
        member _.GetStatus(_token: int16) = raiseNotImpl ()
        member _.OnCompleted(_continuation, _state, _token, _flags) = raiseNotImpl ()

    interface IValueTaskSource<bool> with
        member _.GetStatus(_token: int16) = raiseNotImpl ()
        member _.GetResult(_token: int16) = raiseNotImpl ()
        member _.OnCompleted(_continuation, _state, _token, _flags) = raiseNotImpl ()

and [<NoComparison; NoEquality>] TaskSeq<'Machine, 'T
    when 'Machine :> IAsyncStateMachine and 'Machine :> IResumableStateMachine<TaskSeqStateMachineData<'T>>>() =
    inherit TaskSeqBase<'T>()
    let initialThreadId = Environment.CurrentManagedThreadId

    /// Shadows the initial machine, just after it is initialized by the F# compiler-generated state.
    /// Used on GetAsyncEnumerator, to ensure a clean state, and a ResumptionPoint of 0.
    [<DefaultValue(false)>]
    val mutable _initialMachine: 'Machine

    /// Keeps the active state machine.
    [<DefaultValue(false)>]
    val mutable _machine: 'Machine

    member this.InitMachineData(ct, machine: 'Machine byref) =
        let data = TaskSeqStateMachineData()
        data.boxedSelf <- this
        data.cancellationToken <- ct
        data.builder <- AsyncIteratorMethodBuilder.Create()
        machine.Data <- data

    // Note: Not entirely clear if this is needed, everything still compiles without it
    interface IValueTaskSource with
        member this.GetResult token =
            let canMoveNext = this._machine.Data.promiseOfValueOrEnd.GetResult token

            if not canMoveNext then
                // see below in generic version for explanation
                this._machine.Data.completed <- true

        member this.GetStatus token = this._machine.Data.promiseOfValueOrEnd.GetStatus token

        member this.OnCompleted(continuation, state, token, flags) =
            this._machine.Data.promiseOfValueOrEnd.OnCompleted(continuation, state, token, flags)

    // Needed for MoveNextAsync to return a ValueTask, this manages the source of the ValueTask
    // in combination with the ManualResetValueTaskSourceCore (in promiseOfValueOrEnd).
    interface IValueTaskSource<bool> with
        member this.GetStatus token = this._machine.Data.promiseOfValueOrEnd.GetStatus token

        /// Returning the boolean value that is used as a result for MoveNextAsync()
        member this.GetResult token =
            let canMoveNext = this._machine.Data.promiseOfValueOrEnd.GetResult token

            // This ensures that, esp. in cases where there's no actual iteration (i.e. empty seq)
            // we can still detect completeness and prevent an incorrect jump in the resumable code.
            // See https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/54
            if not canMoveNext then
                // Signal we reached the end.
                // DO NOT call Data.builder.Complete() here, ONLY do that in the Run method.
                this._machine.Data.completed <- true

            canMoveNext

        member this.OnCompleted(continuation, state, token, flags) =
            this._machine.Data.promiseOfValueOrEnd.OnCompleted(continuation, state, token, flags)

    interface IAsyncStateMachine with
        /// The MoveNext method is called by builder.MoveNext() in the resumable code
        member this.MoveNext() = moveNextRef &this._machine

        /// SetStatemachine is (currently) never called
        member _.SetStateMachine(_state) = () // not needed for reference type

    interface IAsyncEnumerable<'T> with
        member this.GetAsyncEnumerator(ct) =
            // if this is null, it means it's the first time for this Enumerable to create an Enumerator
            // so, to prevent extra allocations, we just return 'self', with the iterator vars set appropriately.
            match this._machine.Data :> obj with
            | null when initialThreadId = Environment.CurrentManagedThreadId ->
                this.InitMachineData(ct, &this._machine)
                this // just return 'self' here

            | _ ->
                Debug.logInfo "GetAsyncEnumerator, start cloning..."

                // We need to reset state, but only to the "initial machine", resetting the _machine to
                // Unchecked.defaultof<_> is wrong, as the compiler uses this to track state. However,
                // we do need a zeroed ResumptionPoint, otherwise we would continue after the last iteration
                // returning an empty sequence.
                //
                // Solution: we shadow the initial machine, which we then re-assign here:
                //
                let clone = TaskSeq<'Machine, 'T>() // we used MemberwiseClone, TODO: test difference in perf, but this should be faster

                // _machine will change, _initialMachine will not, which can be used in a new clone.
                // we still need to copy _initialMachine, as it has been initialized by the F# compiler in AfterCode<_, _>.
                clone._machine <- this._initialMachine
                clone._initialMachine <- this._initialMachine // TODO: proof with a test that this is necessary: probably not
                clone.InitMachineData(ct, &clone._machine)
                Debug.logInfo "GetAsyncEnumerator, finished cloning..."
                clone

    interface System.Collections.Generic.IAsyncEnumerator<'T> with
        member this.Current =
            match this._machine.Data.current with
            | ValueSome x -> x
            | ValueNone ->
                // Returning a default value is similar to how F#'s seq<'T> behaves
                // According to the docs, behavior is Unspecified in case of a call
                // to Current, which means that this is certainly fine, and arguably
                // better than raising an exception.
                Unchecked.defaultof<'T>

        member this.MoveNextAsync() =
            Debug.logInfo "MoveNextAsync..."

            if this._machine.ResumptionPoint = -1 then // can't use as IAsyncEnumerator before IAsyncEnumerable
                Debug.logInfo "at MoveNextAsync: Resumption point = -1"

                ValueTask.False

            elif this._machine.Data.completed then
                Debug.logInfo "at MoveNextAsync: completed = true"

                // return False when beyond the last item
                this._machine.Data.promiseOfValueOrEnd.Reset()
                ValueTask.False

            else
                Debug.logInfo "at MoveNextAsync: normal resumption scenario"

                let data = this._machine.Data
                data.promiseOfValueOrEnd.Reset()
                let mutable ts = this

                Debug.logInfo "at MoveNextAsync: start calling builder.MoveNext()"

                data.builder.MoveNext(&ts)

                Debug.logInfo "at MoveNextAsync: finished calling builder.MoveNext()"

                this.MoveNextAsyncResult()

        /// Disposes of the IAsyncEnumerator (*not* the IAsyncEnumerable!!!)
        member this.DisposeAsync() =
            task {
                Debug.logInfo "DisposeAsync..."

                match this._machine.Data.disposalStack with
                | null -> ()
                | _ ->
                    let mutable exn = None

                    for d in Seq.rev this._machine.Data.disposalStack do
                        try
                            do! d ()
                        with e ->
                            if exn.IsNone then
                                exn <- Some e

                    match exn with
                    | None -> ()
                    | Some e -> raise e
            }
            |> ValueTask


    override this.MoveNextAsyncResult() =
        let data = this._machine.Data
        let version = data.promiseOfValueOrEnd.Version
        let status = data.promiseOfValueOrEnd.GetStatus(version)

        match status with
        | ValueTaskSourceStatus.Succeeded ->
            Debug.logInfo "at MoveNextAsyncResult: case succeeded..."

            let result = data.promiseOfValueOrEnd.GetResult(version)

            if not result then
                // if beyond the end of the stream, ensure we unset
                // the Current value
                data.current <- ValueNone

            ValueTask.FromResult result

        | ValueTaskSourceStatus.Faulted
        | ValueTaskSourceStatus.Canceled
        | ValueTaskSourceStatus.Pending as state ->
            Debug.logInfo ("at MoveNextAsyncResult: case ", state)

            ValueTask.ofIValueTaskSource this version
        | _ ->
            Debug.logInfo "at MoveNextAsyncResult: Unexpected state"
            // assume it's a possibly new, not yet supported case, treat as default
            ValueTask.ofIValueTaskSource this version

and ResumableTSC<'T> = ResumableCode<TaskSeqStateMachineData<'T>, unit>
and TaskSeqStateMachine<'T> = ResumableStateMachine<TaskSeqStateMachineData<'T>>
and TaskSeqResumptionFunc<'T> = ResumptionFunc<TaskSeqStateMachineData<'T>>
and TaskSeqResumptionDynamicInfo<'T> = ResumptionDynamicInfo<TaskSeqStateMachineData<'T>>

type TaskSeqBuilder() =

    member inline _.Delay(f: unit -> ResumableTSC<'T>) = ResumableTSC<'T>(fun sm -> f().Invoke(&sm))

    member inline _.Run(code: ResumableTSC<'T>) : IAsyncEnumerable<'T> =
        if __useResumableCode then
            // This is the static implementation.  A new struct type is created.
            __stateMachine<TaskSeqStateMachineData<'T>, IAsyncEnumerable<'T>>
                // IAsyncStateMachine.MoveNext
                (MoveNextMethodImpl<_>(fun sm ->
                    //-- RESUMABLE CODE START
                    __resumeAt sm.ResumptionPoint

                    try
                        Debug.logInfo "at Run.MoveNext start"

                        let __stack_code_fin = code.Invoke(&sm)

                        if __stack_code_fin then
                            Debug.logInfo $"at Run.MoveNext, done"

                            // Signal we're at the end
                            // NOTE: if we don't do it here, as well as in IValueTaskSource<bool>.GetResult
                            // we either end up in an endless loop, or we'll get NRE on empty sequences.
                            // see: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/54
                            sm.Data.promiseOfValueOrEnd.SetResult(false)
                            sm.Data.builder.Complete()
                            sm.Data.completed <- true

                        elif sm.Data.current.IsSome then
                            Debug.logInfo $"at Run.MoveNext, still more items in enumerator"

                            // Signal there's more data:
                            sm.Data.promiseOfValueOrEnd.SetResult(true)

                        else
                            // Goto request
                            Debug.logInfo $"at Run.MoveNext, await, MoveNextAsync has not completed yet"

                            // don't capture the full object in the next closure (won't work because: byref)
                            // but only a reference to itself.
                            let boxed = sm.Data.boxedSelf

                            sm.Data.awaiter.UnsafeOnCompleted(fun () ->
                                let mutable boxed = boxed
                                moveNextRef &boxed)

                    with exn ->
                        Debug.logInfo ("Setting exception of PromiseOfValueOrEnd to: ", exn.Message)
                        sm.Data.promiseOfValueOrEnd.SetException(exn)
                        sm.Data.builder.Complete()

                //-- RESUMABLE CODE END
                ))
                (SetStateMachineMethodImpl<_>(fun sm state -> ())) // not used in reference impl
                (AfterCode<_, _>(fun sm ->
                    Debug.logInfo "at AfterCode<_, _>, after F# inits the sm, and we can attach extra info"

                    let ts = TaskSeq<TaskSeqStateMachine<'T>, 'T>()
                    ts._initialMachine <- sm
                    ts._machine <- sm
                    ts :> IAsyncEnumerable<'T>))
        else
            //    let initialResumptionFunc = TaskSeqResumptionFunc<'T>(fun sm -> code.Invoke(&sm))
            //    let resumptionFuncExecutor = TaskSeqResumptionExecutor<'T>(fun sm f ->
            //            // TODO: add exception handling?
            //            if f.Invoke(&sm) then
            //                sm.ResumptionPoint <- -2)
            //    let setStateMachine = SetStateMachineMethodImpl<_>(fun sm f -> ())
            //    sm.Machine.ResumptionFuncInfo <- (initialResumptionFunc, resumptionFuncExecutor, setStateMachine)
            //sm.Start()
            NotImplementedException "No dynamic implementation for TaskSeq yet."
            |> raise


    member inline _.Zero() : ResumableTSC<'T> =
        Debug.logInfo "at Zero()"
        ResumableCode.Zero()

    member inline _.Combine(task1: ResumableTSC<'T>, task2: ResumableTSC<'T>) =
        Debug.logInfo "at Combine(.., ..)"

        ResumableCode.Combine(task1, task2)

    /// Used by `For`. F# currently doesn't support `while!`, so this cannot be called directly from the CE
    member inline _.WhileAsync([<InlineIfLambda>] condition: unit -> ValueTask<bool>, body: ResumableTSC<'T>) : ResumableTSC<'T> =
        let mutable condition_res = true

        ResumableCode.While(
            (fun () -> condition_res),
            ResumableTSC<'T>(fun sm ->
                let mutable __stack_condition_fin = true
                let __stack_vtask = condition ()

                if __stack_vtask.IsCompleted then
                    Debug.logInfo "at WhileAsync: returning completed task"

                    __stack_condition_fin <- true
                    condition_res <- __stack_vtask.Result
                else
                    Debug.logInfo "at WhileAsync: awaiting non-completed task"

                    let task = __stack_vtask.AsTask()
                    let mutable awaiter = task.GetAwaiter()
                    // This will yield with __stack_fin = false
                    // This will resume with __stack_fin = true
                    let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                    __stack_condition_fin <- __stack_yield_fin

                    if __stack_condition_fin then
                        condition_res <- task.Result
                    else
                        sm.Data.awaiter <- awaiter
                        sm.Data.current <- ValueNone

                if __stack_condition_fin then
                    if condition_res then body.Invoke(&sm) else true
                else
                    false)
        )

    member inline _.While([<InlineIfLambda>] condition: unit -> bool, body: ResumableTSC<'T>) =
        Debug.logInfo "at While(...)"
        ResumableCode.While(condition, body)

    member inline _.TryWith(body: ResumableTSC<'T>, catch: exn -> ResumableTSC<'T>) = ResumableCode.TryWith(body, catch)

    member inline _.TryFinallyAsync(body: ResumableTSC<'T>, compensationAction: unit -> Task) =
        ResumableCode.TryFinallyAsync(

            ResumableTSC<'T>(fun sm ->
                sm.Data.PushDispose compensationAction
                body.Invoke(&sm)),

            ResumableTSC<'T>(fun sm ->

                sm.Data.PopDispose()
                let mutable __stack_condition_fin = true
                let __stack_vtask = compensationAction ()

                if not __stack_vtask.IsCompleted then
                    let mutable awaiter = __stack_vtask.GetAwaiter()
                    let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                    __stack_condition_fin <- __stack_yield_fin

                    if not __stack_condition_fin then
                        sm.Data.awaiter <- awaiter

                __stack_condition_fin)
        )

    member inline _.TryFinally(body: ResumableTSC<'T>, compensationAction: unit -> unit) =
        ResumableCode.TryFinally(
            ResumableTSC<'T>(fun sm ->
                sm.Data.PushDispose(compensationAction >> Task.get_CompletedTask)
                body.Invoke(&sm)),

            ResumableTSC<'T>(fun sm ->
                sm.Data.PopDispose()
                compensationAction ()
                true)
        )

    member inline this.Using(disp: #IAsyncDisposable, body: #IAsyncDisposable -> ResumableTSC<'T>) =

        // A using statement is just a try/finally with the finally block disposing if non-null.
        this.TryFinallyAsync(
            (fun sm -> (body disp).Invoke(&sm)),
            (fun () ->
                if not (isNull (box disp)) then
                    disp.DisposeAsync().AsTask()
                else
                    Task.CompletedTask)
        )

    member inline _.Yield(value: 'T) : ResumableTSC<'T> =
        ResumableTSC<'T>(fun sm ->
            // This will yield with __stack_fin = false
            // This will resume with __stack_fin = true
            Debug.logInfo "at Yield"

            let __stack_fin = ResumableCode.Yield().Invoke(&sm)
            sm.Data.current <- ValueSome value
            sm.Data.awaiter <- null
            __stack_fin)

//
// These "modules of priority" allow for an indecisive F# to resolve
// the proper overload if a single type implements more than one
// interface. For instance, a type implementing 'IDisposable' and
// 'IAsyncDisposable'.
//
// See for more info tasks.fs in F# Core.
//
// This section also includes the dependencies of such overloads
// (like For depending on Using etc).
//

[<AutoOpen>]
module LowPriority =
    type TaskSeqBuilder with

        //
        // Note: we cannot place _.Bind directly on the type, as the NoEagerXXX attribute
        // has no effect, and each use of `do!` will give an overload error (because the
        // `TaskLike` type and the `Task<_>` type are partially interchangeable, see notes there).
        //
        // However, we cannot unify these two methods, because Task<_> inherits from Task (non-generic)
        // and we need a way to distinguish these two methods.
        //
        // Types handled:
        //  - ValueTask (non-generic, because it implements GetResult() -> unit)
        //  - ValueTask<'T> (because it implements GetResult() -> 'TResult)
        //  - Task (non-generic, because it implements GetResult() -> unit)
        //  - any other type that implements GetAwaiter()
        //
        // Not handled:
        //  - Task<'T> (because it only implements GetResult() -> unit, not GetResult() -> 'TResult)

        [<NoEagerConstraintApplication>]
        member inline _.Bind< ^TaskLike, 'T, 'U, ^Awaiter, 'TOverall
            when ^TaskLike: (member GetAwaiter: unit -> ^Awaiter)
            and ^Awaiter :> ICriticalNotifyCompletion
            and ^Awaiter: (member get_IsCompleted: unit -> bool)
            and ^Awaiter: (member GetResult: unit -> 'T)>
            (
                task: ^TaskLike,
                continuation: ('T -> ResumableTSC<'U>)
            ) =

            ResumableTSC<'U>(fun sm ->
                let mutable awaiter = (^TaskLike: (member GetAwaiter: unit -> ^Awaiter) (task))
                let mutable __stack_fin = true

                Debug.logInfo "at TaskLike bind"

                if not (^Awaiter: (member get_IsCompleted: unit -> bool) awaiter) then
                    // This will yield with __stack_fin2 = false
                    // This will resume with __stack_fin2 = true
                    let __stack_fin2 = ResumableCode.Yield().Invoke(&sm)
                    __stack_fin <- __stack_fin2

                Debug.logInfo ("at TaskLike bind: this.completed = ", sm.Data.completed)

                if __stack_fin then
                    Debug.logInfo "at TaskLike bind!: finished awaiting, calling continuation"
                    let result = (^Awaiter: (member GetResult: unit -> 'T) awaiter)
                    (continuation result).Invoke(&sm)

                else
                    Debug.logInfo "at TaskLike bind: await further"

                    sm.Data.awaiter <- awaiter
                    sm.Data.current <- ValueNone
                    false)


[<AutoOpen>]
module MediumPriority =
    type TaskSeqBuilder with

        member inline this.Using(dispensation: #IDisposable, body: #IDisposable -> ResumableTSC<'T>) =

            // A using statement is just a try/finally with the finally block disposing if non-null.
            this.TryFinally(
                (fun sm -> (body dispensation).Invoke(&sm)),
                (fun () ->
                    // yes, this can be null from time to time
                    if not (isNull (box dispensation)) then
                        dispensation.Dispose())
            )

        member inline this.For(sequence: seq<'TElement>, body: 'TElement -> ResumableTSC<'T>) =
            // A for loop is just a using statement on the sequence's enumerator...
            this.Using(
                sequence.GetEnumerator(),
                // ... and its body is a while loop that advances the enumerator and runs the body on each element.
                fun e -> this.While(e.MoveNext, (fun sm -> (body e.Current).Invoke(&sm)))
            )

        member inline this.YieldFrom(source: seq<'T>) : ResumableTSC<'T> = this.For(source, this.Yield)

        member inline this.For(source: #IAsyncEnumerable<'TElement>, body: 'TElement -> ResumableTSC<'T>) =
            ResumableTSC<'T>(fun sm ->
                this
                    .Using(
                        source.GetAsyncEnumerator(sm.Data.cancellationToken),
                        fun e -> this.WhileAsync(e.MoveNextAsync, (fun sm -> (body e.Current).Invoke(&sm)))
                    )
                    .Invoke(&sm))

        member inline this.YieldFrom(source: IAsyncEnumerable<'T>) = this.For(source, (fun v -> this.Yield(v)))

[<AutoOpen>]
module HighPriority =
    type TaskSeqBuilder with

        //
        // Notes Task:
        //  - Task<_> implements GetAwaiter(), but TaskAwaiter does not implement GetResult() -> TResult
        //  - Instead, it has GetResult() -> unit, which is not '^TaskLike'
        //  - Conclusion: we need an extra high-prio overload to allow support for Task<_>
        //
        // Notes ValueTask:
        //  - In contrast, ValueTask<_> *does have* GetResult() -> 'TResult
        //  - Conclusion: we do not need an extra overload anymore for ValueTask
        //
        member inline _.Bind(task: Task<'T>, continuation: ('T -> ResumableTSC<'U>)) =
            ResumableTSC<'U>(fun sm ->
                let mutable awaiter = task.GetAwaiter()
                let mutable __stack_fin = true

                Debug.logInfo "at Bind"

                if not awaiter.IsCompleted then
                    // This will yield with __stack_fin2 = false
                    // This will resume with __stack_fin2 = true
                    let __stack_fin2 = ResumableCode.Yield().Invoke(&sm)
                    __stack_fin <- __stack_fin2

                Debug.logInfo ("at Bind: with __stack_fin = ", __stack_fin)
                Debug.logInfo ("at Bind: this.completed = ", sm.Data.completed)

                if __stack_fin then
                    Debug.logInfo "at Bind: finished awaiting, calling continuation"
                    let result = awaiter.GetResult()
                    (continuation result).Invoke(&sm)

                else
                    Debug.logInfo "at Bind: await further"

                    sm.Data.awaiter <- awaiter
                    sm.Data.current <- ValueNone
                    false)

        member inline _.Bind(computation: Async<'T>, continuation: ('T -> ResumableTSC<'U>)) =
            ResumableTSC<'U>(fun sm ->
                let mutable awaiter =
                    Async
                        .StartAsTask(computation, cancellationToken = sm.Data.cancellationToken)
                        .GetAwaiter()

                let mutable __stack_fin = true

                Debug.logInfo "at Bind"

                if not awaiter.IsCompleted then
                    // This will yield with __stack_fin2 = false
                    // This will resume with __stack_fin2 = true
                    let __stack_fin2 = ResumableCode.Yield().Invoke(&sm)
                    __stack_fin <- __stack_fin2

                Debug.logInfo ("at Bind: with __stack_fin = ", __stack_fin)
                Debug.logInfo ("at Bind: this.completed = ", sm.Data.completed)

                if __stack_fin then
                    Debug.logInfo "at Bind: finished awaiting, calling continuation"
                    let result = awaiter.GetResult()
                    (continuation result).Invoke(&sm)

                else
                    Debug.logInfo "at Bind: await further"

                    sm.Data.awaiter <- awaiter
                    sm.Data.current <- ValueNone
                    false)

[<AutoOpen>]
module TaskSeqBuilder =
    /// Builds an asynchronous task sequence based on IAsyncEnumerable<'T> using computation expression syntax.
    let taskSeq = TaskSeqBuilder()
