namespace FSharpy.TaskSeqBuilders

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
    let verbose =
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


    /// Call MoveNext on an IAsyncStateMachine by reference
    let inline moveNextRef (x: byref<'T> when 'T :> IAsyncStateMachine) = x.MoveNext()

    // F# requires that we implement interfaces even on an abstract class
    let inline raiseNotImpl () =
        NotImplementedException "Abstract Class: method or property not implemented"
        |> raise

type taskSeq<'T> = IAsyncEnumerable<'T>

type IPriority1 =
    interface
    end

type IPriority2 =
    interface
    end

[<NoComparison; NoEquality>]
type TaskSeqStateMachineData<'T>() =
    member this.LogDump() =
        printfn "    CancellationToken: %A" this.cancellationToken

        printfn
            "    Disposal stack count: %A"
            (if isNull this.disposalStack then
                 0
             else
                 this.disposalStack.Count)

        printfn "    Awaiter: %A" this.awaiter

        printfn "    Promise status: %A"
        <| this.promiseOfValueOrEnd.GetStatus(this.promiseOfValueOrEnd.Version)

        printfn "    Builder hash: %A" <| this.builder.GetHashCode()
        printfn "    Taken: %A" this.taken
        printfn "    Completed: %A" this.completed
        printfn "    Current: %A" this.current

    [<DefaultValue(false)>]
    val mutable cancellationToken: CancellationToken

    [<DefaultValue(false)>]
    val mutable disposalStack: ResizeArray<(unit -> Task)>

    [<DefaultValue(false)>]
    val mutable awaiter: ICriticalNotifyCompletion

    [<DefaultValue(false)>]
    val mutable promiseOfValueOrEnd: ManualResetValueTaskSourceCore<bool>

    [<DefaultValue(false)>]
    val mutable builder: AsyncIteratorMethodBuilder

    [<DefaultValue(false)>]
    val mutable taken: bool

    [<DefaultValue(false)>]
    val mutable completed: bool

    [<DefaultValue(false)>]
    val mutable current: ValueOption<'T>

    [<DefaultValue(false)>]
    val mutable boxed: TaskSeq<'T>
    // For tailcalls using 'return!'
    [<DefaultValue(false)>]
    val mutable tailcallTarget: TaskSeq<'T> option

    member data.PushDispose(disposer: unit -> Task) =
        if isNull data.disposalStack then
            data.disposalStack <- ResizeArray()

        data.disposalStack.Add disposer

    member data.PopDispose() =
        if not (isNull data.disposalStack) then
            data.disposalStack.RemoveAt(data.disposalStack.Count - 1)

and [<AbstractClass; NoEquality; NoComparison>] TaskSeq<'T>() =

    abstract TailcallTarget: TaskSeq<'T> option
    abstract MoveNextAsyncResult: unit -> ValueTask<bool>

    interface IAsyncEnumerator<'T> with
        member _.Current = raiseNotImpl ()
        member _.MoveNextAsync() = raiseNotImpl ()

    interface IAsyncDisposable with
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

    [<DefaultValue(false)>]
    val mutable Machine: 'Machine

    member internal this.hijack() =
        let res = this.Machine.Data.tailcallTarget

        match res with
        | Some tg ->
            // we get here only when there are multiple returns (it seems)
            // hence the tailcall logic
            match tg.TailcallTarget with
            | None -> res
            | (Some tg2 as res2) ->
                // Cut out chains of tailcalls
                this.Machine.Data.tailcallTarget <- Some tg2
                res2
        | None -> res

    // Note: Not entirely clear if this is needed, everything still compiles without it
    interface IValueTaskSource with
        member this.GetResult(token: int16) =
            match this.hijack () with
            | Some tg -> (tg :> IValueTaskSource).GetResult(token)
            | None ->
                this.Machine.Data.promiseOfValueOrEnd.GetResult(token)
                |> ignore

        member this.GetStatus(token: int16) =
            match this.hijack () with
            | Some tg -> (tg :> IValueTaskSource<bool>).GetStatus(token)
            | None -> this.Machine.Data.promiseOfValueOrEnd.GetStatus(token)

        member this.OnCompleted(continuation, state, token, flags) =
            match this.hijack () with
            | Some tg -> (tg :> IValueTaskSource).OnCompleted(continuation, state, token, flags)
            | None -> this.Machine.Data.promiseOfValueOrEnd.OnCompleted(continuation, state, token, flags)

    // Needed for MoveNextAsync to return a ValueTask
    interface IValueTaskSource<bool> with
        member this.GetStatus(token: int16) =
            match this.hijack () with
            | Some tg -> (tg :> IValueTaskSource<bool>).GetStatus(token)
            | None -> this.Machine.Data.promiseOfValueOrEnd.GetStatus(token)

        member this.GetResult(token: int16) =
            match this.hijack () with
            | Some tg ->
                if verbose then
                    printfn
                        "Getting result for token on 'Some' branch, status: %A"
                        ((tg :> IValueTaskSource<bool>).GetStatus(token))

                (tg :> IValueTaskSource<bool>).GetResult(token)
            | None ->
                try
                    if verbose then
                        printfn
                            "Getting result for token on 'None' branch, status: %A"
                            (this.Machine.Data.promiseOfValueOrEnd.GetStatus(token))

                    this.Machine.Data.promiseOfValueOrEnd.GetResult(token)
                with e ->
                    // FYI: an exception here is usually caused by the CE statement (user code) throwing an exception
                    // We're just logging here because the following error would also be caught right here:
                    // "An attempt was made to transition a task to a final state when it had already completed."
                    if verbose then
                        printfn "Error '%s' for token: %i" e.Message token

                    reraise ()

        member this.OnCompleted(continuation, state, token, flags) =
            match this.hijack () with
            | Some tg -> (tg :> IValueTaskSource<bool>).OnCompleted(continuation, state, token, flags)
            | None -> this.Machine.Data.promiseOfValueOrEnd.OnCompleted(continuation, state, token, flags)

    interface IAsyncStateMachine with
        /// The MoveNext method is called by builder.MoveNext() in the resumable code
        member this.MoveNext() =
            match this.hijack () with
            | Some tg ->
                // jump to the hijacked method
                (tg :> IAsyncStateMachine).MoveNext()
            | None -> moveNextRef &this.Machine

        /// SetStatemachine is (currently) never called
        member _.SetStateMachine(_state) =
            if verbose then
                printfn "Setting state machine -- ignored"

            () // not needed for reference type

    interface IAsyncEnumerable<'T> with
        member this.GetAsyncEnumerator(ct) =
            let data = this.Machine.Data

            if
                (not data.taken
                 && initialThreadId = Environment.CurrentManagedThreadId)
            then
                //let clone = this.MemberwiseClone() :?> TaskSeq<'Machine, 'T>
                let data = this.Machine.Data
                data.taken <- true
                data.cancellationToken <- ct
                data.builder <- AsyncIteratorMethodBuilder.Create()

                if verbose then
                    printfn "All data (no clone):"
                    data.LogDump()

                if verbose then
                    printfn "No cloning, resumption point: %i" this.Machine.ResumptionPoint

                this :> IAsyncEnumerator<_>
            else
                if verbose then
                    printfn "GetAsyncEnumerator, cloning..."

                if verbose then
                    printfn "All data before clone:"
                    data.LogDump()

                // it appears that the issue is possibly caused by the problem
                // of having ValueTask all over the place, and by going over the
                // iteration twice, we are trying to *await* twice, which is not allowed
                // see, for instance: https://itnext.io/why-can-a-valuetask-only-be-awaited-once-31169b324fa4
                let clone = this.MemberwiseClone() :?> TaskSeq<'Machine, 'T>
                data.taken <- true

                // Explanation for resetting Machine use brute-force
                //
                // This appears to fix the problem that ResumptionPoint was not reset. I'd prefer
                // a less drastical method. It solves a scenario like the following:
                // let ts = taskSeq { yield 1; yield 2 }
                // let e1 = ts.GetAsyncEnumerator()
                // let! hasNext = e.MoveNextAsync()
                // let e2 = ts.GetAsyncEnumerator()
                // let! hasNext = e.MoveNextAsync()  // without this hack, it would continue where e1 left off
                // let a = e1.Current
                // let b = e2.Current
                // let isTrue = a = b  // true with this, false without it
                clone.Machine <- Unchecked.defaultof<_>
                //clone.Machine.ResumptionPoint <- 0

                // the following lines just re-initialize the key data fields State.
                clone.Machine.Data <- TaskSeqStateMachineData()
                clone.Machine.Data.cancellationToken <- ct
                clone.Machine.Data.taken <- true
                clone.Machine.Data.builder <- AsyncIteratorMethodBuilder.Create()

                if verbose then
                    printfn "All data after clone:"
                    clone.Machine.Data.LogDump()


                //// calling reset causes NRE in IValueTaskSource.GetResult above
                //clone.Machine.Data.promiseOfValueOrEnd.Reset()
                //clone.Machine.Data.boxed <- clone
                ////clone.Machine.Data.disposalStack <- null // reference type, would otherwise still reference original stack
                //////clone.Machine.Data.tailcallTarget <- Some clone  // this will lead to an SO exception
                //clone.Machine.Data.awaiter <- null
                //clone.Machine.Data.current <- ValueNone
                //clone.Machine.Data.completed <- false

                if verbose then
                    printfn
                        "Cloning, resumption point original: %i, clone: %i"
                        this.Machine.ResumptionPoint
                        clone.Machine.ResumptionPoint

                clone :> System.Collections.Generic.IAsyncEnumerator<'T>

    interface IAsyncDisposable with
        member this.DisposeAsync() =
            match this.hijack () with
            | Some tg -> (tg :> IAsyncDisposable).DisposeAsync()
            | None ->
                if verbose then
                    printfn "DisposeAsync..."

                task {
                    match this.Machine.Data.disposalStack with
                    | null -> ()
                    | _ ->
                        let mutable exn = None

                        for d in Seq.rev this.Machine.Data.disposalStack do
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

    interface System.Collections.Generic.IAsyncEnumerator<'T> with
        member this.Current =
            match this.hijack () with
            | Some tg -> (tg :> IAsyncEnumerator<'T>).Current
            | None ->
                match this.Machine.Data.current with
                | ValueSome x -> x
                | ValueNone ->
                    // Returning a default value is similar to how F#'s seq<'T> behaves
                    // According to the docs, behavior is Unspecified in case of a call
                    // to Current, which means that this is certainly fine, and arguably
                    // better than raising an exception.
                    Unchecked.defaultof<'T>

        member this.MoveNextAsync() =
            match this.hijack () with
            | Some tg -> (tg :> IAsyncEnumerator<'T>).MoveNextAsync()
            | None ->
                if verbose then
                    printfn "MoveNextAsync..."

                if this.Machine.ResumptionPoint = -1 then // can't use as IAsyncEnumerator before IAsyncEnumerable
                    if verbose then
                        printfn "at MoveNextAsync: Resumption point = -1"

                    ValueTask<bool>()

                elif this.Machine.Data.completed then
                    if verbose then
                        printfn "at MoveNextAsync: completed = true"

                    // return False when beyond the last item
                    this.Machine.Data.promiseOfValueOrEnd.Reset()
                    ValueTask<bool>()

                else
                    if verbose then
                        printfn "at MoveNextAsync: normal resumption scenario"

                    let data = this.Machine.Data
                    data.promiseOfValueOrEnd.Reset()
                    let mutable ts = this

                    if verbose then
                        printfn "at MoveNextAsync: start calling builder.MoveNext()"

                    data.builder.MoveNext(&ts)

                    if verbose then
                        printfn "at MoveNextAsync: done calling builder.MoveNext()"

                    // If the move did a hijack then get the result from the final one
                    match this.hijack () with
                    | Some tg -> tg.MoveNextAsyncResult()
                    | None -> this.MoveNextAsyncResult()


    override this.MoveNextAsyncResult() =
        let data = this.Machine.Data
        let version = data.promiseOfValueOrEnd.Version
        let status = data.promiseOfValueOrEnd.GetStatus(version)

        match status with
        | ValueTaskSourceStatus.Succeeded ->
            if verbose then
                printfn "at MoveNextAsyncResult: case succeeded..."

            let result = data.promiseOfValueOrEnd.GetResult(version)

            if not result then
                // if beyond the end of the stream, ensure we unset
                // the Current value
                data.current <- ValueNone

            ValueTask<bool>(result)

        | ValueTaskSourceStatus.Faulted
        | ValueTaskSourceStatus.Canceled
        | ValueTaskSourceStatus.Pending ->
            if verbose then
                printfn "at MoveNextAsyncResult: case pending/faulted/cancelled..."

            ValueTask<bool>(this, version) // uses IValueTaskSource<'T>
        | _ ->
            if verbose then
                printfn "at MoveNextAsyncResult: Unexpected state"
            // assume it's a possibly new, not yet supported case, treat as default
            ValueTask<bool>(this, version) // uses IValueTaskSource<'T>

    override cr.TailcallTarget = cr.hijack ()

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

                    if verbose then
                        printfn "Resuming at resumption point %i" sm.ResumptionPoint

                    try
                        if verbose then
                            printfn "at Run.MoveNext start"

                        let __stack_code_fin = code.Invoke(&sm)

                        if verbose then
                            printfn $"at Run.MoveNext, __stack_code_fin={__stack_code_fin}"

                        if __stack_code_fin then
                            if verbose then
                                printfn $"at Run.MoveNext, done"

                            sm.Data.promiseOfValueOrEnd.SetResult(false)
                            sm.Data.builder.Complete()
                            sm.Data.completed <- true

                        elif sm.Data.current.IsSome then
                            if verbose then
                                printfn $"at Run.MoveNext, yield"

                            sm.Data.promiseOfValueOrEnd.SetResult(true)

                        else
                            // Goto request
                            match sm.Data.tailcallTarget with
                            | Some tg ->
                                if verbose then
                                    printfn $"at Run.MoveNext, hijack"

                                let mutable tg = tg
                                moveNextRef &tg

                            | None ->
                                if verbose then
                                    printfn $"at Run.MoveNext, await"

                                let boxed = sm.Data.boxed

                                sm.Data.awaiter.UnsafeOnCompleted(
                                    Action(fun () ->
                                        let mutable boxed = boxed
                                        moveNextRef &boxed)
                                )

                    with exn ->
                        if verbose then
                            printfn "Setting exception of PromiseOfValueOrEnd to: %s" exn.Message

                        sm.Data.promiseOfValueOrEnd.SetException(exn)
                        sm.Data.builder.Complete()
                //-- RESUMABLE CODE END
                ))
                (SetStateMachineMethodImpl<_>(fun sm state ->
                    if verbose then
                        printfn "at SetStatemachingMethodImpl, ignored"

                    ()))
                (AfterCode<_, _>(fun sm ->
                    if verbose then
                        printfn "at AfterCode<_, _>, setting the Machine field to the StateMachine"

                    let ts = TaskSeq<TaskSeqStateMachine<'T>, 'T>()
                    ts.Machine <- sm
                    ts.Machine.Data <- TaskSeqStateMachineData()
                    ts.Machine.Data.boxed <- ts
                    ts :> IAsyncEnumerable<'T>))
        else
            failwith "no dynamic implementation as yet"
    //    let initialResumptionFunc = TaskSeqResumptionFunc<'T>(fun sm -> code.Invoke(&sm))
    //    let resumptionFuncExecutor = TaskSeqResumptionExecutor<'T>(fun sm f ->
    //            // TODO: add exception handling?
    //            if f.Invoke(&sm) then
    //                sm.ResumptionPoint <- -2)
    //    let setStateMachine = SetStateMachineMethodImpl<_>(fun sm f -> ())
    //    sm.Machine.ResumptionFuncInfo <- (initialResumptionFunc, resumptionFuncExecutor, setStateMachine)
    //sm.Start()


    member inline _.Zero() : TaskSeqCode<'T> =
        if verbose then
            printfn "at Zero()"

        ResumableCode.Zero()

    member inline _.Combine(task1: TaskSeqCode<'T>, task2: TaskSeqCode<'T>) : TaskSeqCode<'T> =
        if verbose then
            printfn "at Combine(.., ..)"

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
                    if verbose then
                        printfn "at WhileAsync: returning completed task"

                    __stack_condition_fin <- true
                    condition_res <- __stack_vtask.Result
                else
                    if verbose then
                        printfn "at WhileAsync: awaiting non-completed task"

                    let task = __stack_vtask.AsTask()
                    let mutable awaiter = task.GetAwaiter()
                    // This will yield with __stack_fin = false
                    // This will resume with __stack_fin = true
                    let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                    __stack_condition_fin <- __stack_yield_fin

                    if verbose then
                        printfn
                            "at WhileAsync: after Yield().Invoke(sm), __stack_condition_fin=%b"
                            __stack_condition_fin

                    if __stack_condition_fin then
                        condition_res <- task.Result
                    else
                        //if verbose then printfn "calling AwaitUnsafeOnCompleted"
                        sm.Data.awaiter <- awaiter
                        sm.Data.current <- ValueNone

                if __stack_condition_fin then
                    if condition_res then body.Invoke(&sm) else true
                else
                    false)
        )

    member inline b.While([<InlineIfLambda>] condition: unit -> bool, body: TaskSeqCode<'T>) : TaskSeqCode<'T> =
        if verbose then
            printfn "at While(...), calling WhileAsync()"

        b.WhileAsync((fun () -> ValueTask<bool>(condition ())), body)

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
        ignore priority
        // A using statement is just a try/finally with the finally block disposing if non-null.
        this.TryFinally(
            (fun sm -> (body disp).Invoke(&sm)),
            (fun () ->
                if not (isNull (box disp)) then
                    disp.Dispose())
        )

    member inline this.Using
        (
            disp: #IAsyncDisposable,
            body: #IAsyncDisposable -> TaskSeqCode<'T>,
            ?priority: IPriority1
        ) : TaskSeqCode<'T> =
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
            if verbose then
                printfn "at Yield, before Yield().Invoke(sm)"

            let __stack_fin = ResumableCode.Yield().Invoke(&sm)

            if verbose then
                printfn "at Yield, __stack_fin = %b" __stack_fin

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

            if verbose then
                printfn "at Bind"

            if not awaiter.IsCompleted then
                // This will yield with __stack_fin2 = false
                // This will resume with __stack_fin2 = true
                let __stack_fin2 = ResumableCode.Yield().Invoke(&sm)
                __stack_fin <- __stack_fin2

            if verbose then
                printfn "at Bind: with __stack_fin = %b" __stack_fin

            if __stack_fin then
                if verbose then
                    printfn "at Bind: with getting result from awaiter"

                let result = awaiter.GetResult()

                if verbose then
                    printfn "at Bind: calling continuation"

                (continuation result).Invoke(&sm)
            else
                if verbose then
                    printfn "at Bind: calling AwaitUnsafeOnCompleted"

                sm.Data.awaiter <- awaiter
                sm.Data.current <- ValueNone
                false)

    member inline _.Bind(task: ValueTask<'TResult1>, continuation: ('TResult1 -> TaskSeqCode<'T>)) : TaskSeqCode<'T> =
        TaskSeqCode<'T>(fun sm ->
            let mutable awaiter = task.GetAwaiter()
            let mutable __stack_fin = true

            if verbose then
                printfn "at BindV"

            if not awaiter.IsCompleted then
                // This will yield with __stack_fin2 = false
                // This will resume with __stack_fin2 = true
                let __stack_fin2 = ResumableCode.Yield().Invoke(&sm)
                __stack_fin <- __stack_fin2

            if __stack_fin then
                let result = awaiter.GetResult()
                (continuation result).Invoke(&sm)
            else
                if verbose then
                    printfn "at BindV: calling AwaitUnsafeOnCompleted"

                sm.Data.awaiter <- awaiter
                sm.Data.current <- ValueNone
                false)

    // TODO: using return! for tailcalls is wrong.  We should use yield! and have F#
    // desugar to a different builder method when in tailcall position
    //
    // Because of this using return! from non-tailcall position e.g. in a try-finally or try-with will
    // giv incorrect results (escaping the exception handler - 'close up shop and draw results from somewhere else')
    member inline b.ReturnFrom(other: IAsyncEnumerable<'T>) : TaskSeqCode<'T> =
        TaskSeqCode<_>(fun sm ->
            match other with
            | :? TaskSeq<'T> as other ->
                sm.Data.tailcallTarget <- Some other
                sm.Data.awaiter <- null
                sm.Data.current <- ValueNone
                // For tailcalls we return 'false' and re-run from the entry (trampoline)
                false

            | _ -> b.YieldFrom(other).Invoke(&sm))
