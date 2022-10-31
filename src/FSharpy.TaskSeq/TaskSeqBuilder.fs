namespace FSharpy.TaskSeqBuilders

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


[<AutoOpen>]
module Internal = // cannot be marked with 'internal' scope

    /// Setting from environment variable TASKSEQ_LOG_VERBOSE, which,
    /// when set, enables (very) verbose printing of flow and state
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

    type Debug =

        [<DefaultValue(false)>]
        static val mutable private verbose: bool option

        /// Setting from environment variable TASKSEQ_LOG_VERBOSE, which,
        /// when set, enables (very) verbose printing of flow and state
        static member private getVerboseSetting() =
            match Debug.verbose with
            | None ->
                let verboseEnv =
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

                Debug.verbose <- Some verboseEnv
                verboseEnv

            | Some setting -> setting

        [<Conditional("DEBUG")>]
        static member private print value =
            match Debug.getVerboseSetting () with
            | false -> ()
            | true ->
                // don't use ksprintf here, because the compiler does not remove all allocations due to
                // the way PrintfFormat types are compiled, even if we set the Conditional attribute.
                printfn "%i (%b): %s" Thread.CurrentThread.ManagedThreadId Thread.CurrentThread.IsThreadPoolThread value

        [<Conditional("DEBUG")>]
        static member logInfo(str) = Debug.print str

        [<Conditional("DEBUG")>]
        static member logInfo(str, data) = Debug.print $"%s{str}{data}"

    /// Call MoveNext on an IAsyncStateMachine by reference
    let inline moveNextRef (x: byref<'T> when 'T :> IAsyncStateMachine) = x.MoveNext()

    // F# requires that we implement interfaces even on an abstract class
    let inline raiseNotImpl () =
        NotImplementedException "Abstract Class: method or property not implemented"
        |> raise

open type Debug

type taskSeq<'T> = IAsyncEnumerable<'T>

type IPriority1 =
    interface
    end

type IPriority2 =
    interface
    end

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
    val mutable boxedSelf: TaskSeq<'T>

    member data.PushDispose(disposer: unit -> Task) =
        if isNull data.disposalStack then
            data.disposalStack <- null

    //data.disposalStack.Add disposer

    member data.PopDispose() =
        if not (isNull data.disposalStack) then
            data.disposalStack.RemoveAt(data.disposalStack.Count - 1)

and [<AbstractClass; NoEquality; NoComparison>] TaskSeq<'T>() =

    abstract MoveNextAsyncResult: unit -> ValueTask<bool>

    /// Initializes the machine data on 'self'
    abstract InitMachineDataForTailcalls: ct: CancellationToken -> unit

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
    inherit TaskSeq<'T>()
    let initialThreadId = Environment.CurrentManagedThreadId

    /// Shadows the initial machine, just after it is initialized by the F# compiler-generated state.
    /// Used on GetAsyncEnumerator, to ensure a clean state, and a ResumptionPoint of 0.
    [<DefaultValue(false)>]
    val mutable _initialMachine: 'Machine

    /// Keeps the active state machine.
    [<DefaultValue(false)>]
    val mutable _machine: 'Machine

    override this.InitMachineDataForTailcalls(ct) =
        match this._machine.Data :> obj with
        | null -> this.InitMachineData(ct, &this._machine)
        | _ -> ()

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
            // See https://github.com/abelbraaksma/TaskSeq/pull/54
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
                logInfo "GetAsyncEnumerator, start cloning..."

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
                logInfo "GetAsyncEnumerator, finished cloning..."
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
            logInfo "MoveNextAsync..."

            if this._machine.ResumptionPoint = -1 then // can't use as IAsyncEnumerator before IAsyncEnumerable
                logInfo "at MoveNextAsync: Resumption point = -1"

                ValueTask<bool>()

            elif this._machine.Data.completed then
                logInfo "at MoveNextAsync: completed = true"

                // return False when beyond the last item
                this._machine.Data.promiseOfValueOrEnd.Reset()
                ValueTask<bool>()

            else
                logInfo "at MoveNextAsync: normal resumption scenario"

                let data = this._machine.Data
                data.promiseOfValueOrEnd.Reset()
                let mutable ts = this

                logInfo "at MoveNextAsync: start calling builder.MoveNext()"

                data.builder.MoveNext(&ts)

                logInfo "at MoveNextAsync: finished calling builder.MoveNext()"

                this.MoveNextAsyncResult()

        /// Disposes of the IAsyncEnumerator (*not* the IAsyncEnumerable!!!)
        member this.DisposeAsync() =
            task {
                logInfo "DisposeAsync..."

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
            logInfo "at MoveNextAsyncResult: case succeeded..."

            let result = data.promiseOfValueOrEnd.GetResult(version)

            if not result then
                // if beyond the end of the stream, ensure we unset
                // the Current value
                data.current <- ValueNone

            ValueTask<bool>(result)

        | ValueTaskSourceStatus.Faulted
        | ValueTaskSourceStatus.Canceled
        | ValueTaskSourceStatus.Pending as state ->
            logInfo ("at MoveNextAsyncResult: case ", state)

            ValueTask<bool>(this, version) // uses IValueTaskSource<'T>
        | _ ->
            logInfo "at MoveNextAsyncResult: Unexpected state"
            // assume it's a possibly new, not yet supported case, treat as default
            ValueTask<bool>(this, version) // uses IValueTaskSource<'T>

and TaskSeqCode<'T> = ResumableCode<TaskSeqStateMachineData<'T>, unit>
and TaskSeqStateMachine<'T> = ResumableStateMachine<TaskSeqStateMachineData<'T>>
and TaskSeqResumptionFunc<'T> = ResumptionFunc<TaskSeqStateMachineData<'T>>
and TaskSeqResumptionDynamicInfo<'T> = ResumptionDynamicInfo<TaskSeqStateMachineData<'T>>

type TaskSeqBuilder() =

    member inline _.Delay(f: unit -> TaskSeqCode<'T>) : TaskSeqCode<'T> = TaskSeqCode<'T>(fun sm -> f().Invoke(&sm))

    member inline _.Run(code: TaskSeqCode<'T>) : IAsyncEnumerable<'T> =
        if __useResumableCode then
            // This is the static implementation.  A new struct type is created.
            __stateMachine<TaskSeqStateMachineData<'T>, IAsyncEnumerable<'T>>
                // IAsyncStateMachine.MoveNext
                (MoveNextMethodImpl<_>(fun sm ->
                    //-- RESUMABLE CODE START
                    __resumeAt sm.ResumptionPoint

                    try
                        logInfo "at Run.MoveNext start"

                        let __stack_code_fin = code.Invoke(&sm)

                        if __stack_code_fin then
                            logInfo $"at Run.MoveNext, done"

                            // Signal we're at the end
                            // NOTE: if we don't do it here, as well as in IValueTaskSource<bool>.GetResult
                            // we either end up in an endless loop, or we'll get NRE on empty sequences.
                            // see: https://github.com/abelbraaksma/TaskSeq/pull/54
                            sm.Data.promiseOfValueOrEnd.SetResult(false)
                            sm.Data.builder.Complete()
                            sm.Data.completed <- true

                        elif sm.Data.current.IsSome then
                            logInfo $"at Run.MoveNext, still more items in enumerator"

                            // Signal there's more data:
                            sm.Data.promiseOfValueOrEnd.SetResult(true)

                        else
                            // Goto request
                            logInfo $"at Run.MoveNext, await, MoveNextAsync has not completed yet"

                            // don't capture the full object in the next closure (won't work because: byref)
                            // but only a reference to itself.
                            let boxed = sm.Data.boxedSelf

                            sm.Data.awaiter.UnsafeOnCompleted(
                                Action(fun () ->
                                    let mutable boxed = boxed
                                    moveNextRef &boxed)
                            )

                    with exn ->
                        logInfo ("Setting exception of PromiseOfValueOrEnd to: ", exn.Message)
                        sm.Data.promiseOfValueOrEnd.SetException(exn)
                        sm.Data.builder.Complete()

                //-- RESUMABLE CODE END
                ))
                (SetStateMachineMethodImpl<_>(fun sm state -> ())) // not used in reference impl
                (AfterCode<_, _>(fun sm ->
                    logInfo "at AfterCode<_, _>, after F# inits the sm, and we can attach extra info"

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


    member inline _.Zero() : TaskSeqCode<'T> =
        logInfo "at Zero()"
        ResumableCode.Zero()

    member inline _.Combine(task1: TaskSeqCode<'T>, task2: TaskSeqCode<'T>) : TaskSeqCode<'T> =
        logInfo "at Combine(.., ..)"

        ResumableCode.Combine(task1, task2)

    member inline _.WhileAsync
        (
            [<InlineIfLambda>] condition: unit -> ValueTask<bool>,
            body: TaskSeqCode<'T>
        ) : TaskSeqCode<'T> =
        let mutable condition_res = true

        ResumableCode.While(
            (fun () -> condition_res),
            ResumableCode<_, _>(fun sm ->
                let mutable __stack_condition_fin = true
                let __stack_vtask = condition ()

                if __stack_vtask.IsCompleted then
                    logInfo "at WhileAsync: returning completed task"

                    __stack_condition_fin <- true
                    condition_res <- __stack_vtask.Result
                else
                    logInfo "at WhileAsync: awaiting non-completed task"

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

    member inline b.While([<InlineIfLambda>] condition: unit -> bool, body: TaskSeqCode<'T>) : TaskSeqCode<'T> =
        logInfo "at While(...)"

        // was this:
        // b.WhileAsync((fun () -> ValueTask<bool>(condition ())), body)
        ResumableCode.While(condition, body)

    member inline _.TryWith(body: TaskSeqCode<'T>, catch: exn -> TaskSeqCode<'T>) : TaskSeqCode<'T> =
        ResumableCode.TryWith(body, catch)

    member inline _.TryFinallyAsync(body: TaskSeqCode<'T>, compensation: unit -> Task) : TaskSeqCode<'T> =
        ResumableCode.TryFinallyAsync(

            TaskSeqCode<'T>(fun sm ->
                sm.Data.PushDispose(fun () -> compensation ())
                body.Invoke(&sm)),

            ResumableCode<_, _>(fun sm ->

                sm.Data.PopDispose()
                let mutable __stack_condition_fin = true
                let __stack_vtask = compensation ()

                if not __stack_vtask.IsCompleted then
                    let mutable awaiter = __stack_vtask.GetAwaiter()
                    let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                    __stack_condition_fin <- __stack_yield_fin

                    if not __stack_condition_fin then
                        sm.Data.awaiter <- awaiter

                __stack_condition_fin)
        )

    member inline _.TryFinally(body: TaskSeqCode<'T>, compensation: unit -> unit) : TaskSeqCode<'T> =
        ResumableCode.TryFinally(
            TaskSeqCode<'T>(fun sm ->
                sm.Data.PushDispose(fun () ->
                    compensation ()
                    Task.CompletedTask)

                body.Invoke(&sm)),

            ResumableCode<_, _>(fun sm ->
                sm.Data.PopDispose()
                compensation ()
                true)
        )

    member inline this.Using
        (
            disp: #IDisposable,
            body: #IDisposable -> TaskSeqCode<'T>,
            ?priority: IPriority2
        ) : TaskSeqCode<'T> =

        // FIXME: what about priority?
        ignore priority

        // A using statement is just a try/finally with the finally block disposing if non-null.
        this.TryFinally(
            (fun sm -> (body disp).Invoke(&sm)),
            (fun () ->
                // yes, this can be null from time to time
                if not (isNull (box disp)) then
                    disp.Dispose())
        )

    member inline this.Using
        (
            disp: #IAsyncDisposable,
            body: #IAsyncDisposable -> TaskSeqCode<'T>,
            ?priority: IPriority1
        ) : TaskSeqCode<'T> =

        // FIXME: what about priorities?
        ignore priority

        // A using statement is just a try/finally with the finally block disposing if non-null.
        this.TryFinallyAsync(
            (fun sm -> (body disp).Invoke(&sm)),
            (fun () ->
                if not (isNull (box disp)) then
                    disp.DisposeAsync().AsTask()
                else
                    Task.CompletedTask)
        )

    member inline this.For(sequence: seq<'TElement>, body: 'TElement -> TaskSeqCode<'T>) : TaskSeqCode<'T> =
        // A for loop is just a using statement on the sequence's enumerator...
        this.Using(
            sequence.GetEnumerator(),
            // ... and its body is a while loop that advances the enumerator and runs the body on each element.
            (fun e -> this.While((fun () -> e.MoveNext()), (fun sm -> (body e.Current).Invoke(&sm))))
        )

    member inline this.For(source: #IAsyncEnumerable<'TElement>, body: 'TElement -> TaskSeqCode<'T>) : TaskSeqCode<'T> =
        TaskSeqCode<'T>(fun sm ->
            this
                .Using(
                    source.GetAsyncEnumerator(sm.Data.cancellationToken),
                    (fun e -> this.WhileAsync((fun () -> e.MoveNextAsync()), (fun sm -> (body e.Current).Invoke(&sm))))
                )
                .Invoke(&sm))

    member inline _.Yield(v: 'T) : TaskSeqCode<'T> =
        TaskSeqCode<'T>(fun sm ->
            // This will yield with __stack_fin = false
            // This will resume with __stack_fin = true
            logInfo "at Yield"

            let __stack_fin = ResumableCode.Yield().Invoke(&sm)
            sm.Data.current <- ValueSome v
            sm.Data.awaiter <- null
            __stack_fin)

    member inline this.YieldFrom(source: IAsyncEnumerable<'T>) : TaskSeqCode<'T> =
        this.For(source, (fun v -> this.Yield(v)))

    member inline this.YieldFrom(source: seq<'T>) : TaskSeqCode<'T> = this.For(source, (fun v -> this.Yield(v)))

    member inline _.Bind(task: Task<'TResult1>, continuation: ('TResult1 -> TaskSeqCode<'T>)) : TaskSeqCode<'T> =
        TaskSeqCode<'T>(fun sm ->
            let mutable awaiter = task.GetAwaiter()
            let mutable __stack_fin = true

            logInfo "at Bind"

            if not awaiter.IsCompleted then
                // This will yield with __stack_fin2 = false
                // This will resume with __stack_fin2 = true
                let __stack_fin2 = ResumableCode.Yield().Invoke(&sm)
                __stack_fin <- __stack_fin2

            logInfo ("at Bind: with __stack_fin = ", __stack_fin)
            logInfo ("at Bind: this.completed = ", sm.Data.completed)

            if __stack_fin then
                let result = awaiter.GetResult()
                (continuation result).Invoke(&sm)

            else
                logInfo "at Bind: calling AwaitUnsafeOnCompleted"

                sm.Data.awaiter <- awaiter
                sm.Data.current <- ValueNone
                false)

    member inline _.Bind(task: ValueTask<'TResult1>, continuation: ('TResult1 -> TaskSeqCode<'T>)) : TaskSeqCode<'T> =
        TaskSeqCode<'T>(fun sm ->
            let mutable awaiter = task.GetAwaiter()
            let mutable __stack_fin = true

            logInfo "at BindV"

            if not awaiter.IsCompleted then
                // This will yield with __stack_fin2 = false
                // This will resume with __stack_fin2 = true
                let __stack_fin2 = ResumableCode.Yield().Invoke(&sm)
                __stack_fin <- __stack_fin2

            if __stack_fin then
                let result = awaiter.GetResult()
                (continuation result).Invoke(&sm)
            else
                logInfo "at BindV: calling AwaitUnsafeOnCompleted"

                sm.Data.awaiter <- awaiter
                sm.Data.current <- ValueNone
                false)
