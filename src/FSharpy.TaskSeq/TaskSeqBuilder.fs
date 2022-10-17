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
    let verbose = true

    let inline MoveNext (x: byref<'T> when 'T :> IAsyncStateMachine) = x.MoveNext()

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

    member internal ts.hijack() =
        let res = ts.Machine.Data.tailcallTarget

        match res with
        | Some tg ->
            // we get here only when there are multiple returns (it seems)
            // hence the tailcall logic
            match tg.TailcallTarget with
            | None -> res
            | (Some tg2 as res2) ->
                // Cut out chains of tailcalls
                ts.Machine.Data.tailcallTarget <- Some tg2
                res2
        | None -> res

    // Note: Not entirely clear if this is needed, everything still compiles without it
    interface IValueTaskSource with
        member ts.GetResult(token: int16) =
            match ts.hijack () with
            | Some tg -> (tg :> IValueTaskSource).GetResult(token)
            | None ->
                ts.Machine.Data.promiseOfValueOrEnd.GetResult(token)
                |> ignore

        member ts.GetStatus(token: int16) =
            match ts.hijack () with
            | Some tg -> (tg :> IValueTaskSource<bool>).GetStatus(token)
            | None -> ts.Machine.Data.promiseOfValueOrEnd.GetStatus(token)

        member ts.OnCompleted(continuation, state, token, flags) =
            match ts.hijack () with
            | Some tg -> (tg :> IValueTaskSource).OnCompleted(continuation, state, token, flags)
            | None -> ts.Machine.Data.promiseOfValueOrEnd.OnCompleted(continuation, state, token, flags)

    // Needed for MoveNextAsync to return a ValueTask
    interface IValueTaskSource<bool> with
        member ts.GetStatus(token: int16) =
            match ts.hijack () with
            | Some tg -> (tg :> IValueTaskSource<bool>).GetStatus(token)
            | None -> ts.Machine.Data.promiseOfValueOrEnd.GetStatus(token)

        member ts.GetResult(token: int16) =
            match ts.hijack () with
            | Some tg ->
                if verbose then
                    printfn "Getting result for token on 'Some' branch: %i" token

                (tg :> IValueTaskSource<bool>).GetResult(token)
            | None ->
                try
                    if verbose then
                        printfn "Getting result for token on 'None' branch: %i" token

                    ts.Machine.Data.promiseOfValueOrEnd.GetResult(token)
                with e ->
                    if verbose then
                        printfn "Error for token: %i" token

                    //reraise ()
                    true

        member ts.OnCompleted(continuation, state, token, flags) =
            match ts.hijack () with
            | Some tg -> (tg :> IValueTaskSource<bool>).OnCompleted(continuation, state, token, flags)
            | None -> ts.Machine.Data.promiseOfValueOrEnd.OnCompleted(continuation, state, token, flags)

    interface IAsyncStateMachine with
        member ts.MoveNext() =
            match ts.hijack () with
            | Some tg -> (tg :> IAsyncStateMachine).MoveNext()
            | None -> MoveNext(&ts.Machine)

        member _.SetStateMachine(_state) = () // not needed for reference type

    interface IAsyncEnumerable<'T> with
        member ts.GetAsyncEnumerator(ct) =
            let data = ts.Machine.Data

            if
                (not data.taken
                 && initialThreadId = Environment.CurrentManagedThreadId)
            then
                data.taken <- true
                data.cancellationToken <- ct
                data.builder <- AsyncIteratorMethodBuilder.Create()

                if verbose then
                    printfn "No cloning, resumption point: %i" ts.Machine.ResumptionPoint

                (ts :> IAsyncEnumerator<_>)
            else
                if verbose then
                    printfn "GetAsyncEnumerator, cloning..."

                // it appears that the issue is possibly caused by the problem
                // of having ValueTask all over the place, and by going over the
                // iteration twice, we are trying to *await* twice, which is not allowed
                // see, for instance: https://itnext.io/why-can-a-valuetask-only-be-awaited-once-31169b324fa4


                let clone = ts.MemberwiseClone() :?> TaskSeq<'Machine, 'T>
                data.taken <- true
                clone.Machine.Data.cancellationToken <- ct
                clone.Machine.Data.taken <- true
                //clone.Machine.Data.builder <- AsyncIteratorMethodBuilder.Create()
                // calling reset causes NRE in IValueTaskSource.GetResult above
                //clone.Machine.Data.promiseOfValueOrEnd.Reset()
                //clone.Machine.Data.boxed <- clone
                //clone.Machine.Data.disposalStack <- null // reference type, would otherwise still reference original stack
                //clone.Machine.Data.tailcallTarget <- Some clone  // this will lead to an SO exception
                //clone.Machine.Data.awaiter <- null
                //clone.Machine.Data.current <- ValueNone

                if verbose then
                    printfn
                        "Cloning, resumption point original: %i, clone: %i"
                        ts.Machine.ResumptionPoint
                        clone.Machine.ResumptionPoint

                (clone :> System.Collections.Generic.IAsyncEnumerator<'T>)

    interface IAsyncDisposable with
        member ts.DisposeAsync() =
            match ts.hijack () with
            | Some tg -> (tg :> IAsyncDisposable).DisposeAsync()
            | None ->
                if verbose then
                    printfn "DisposeAsync..."

                task {
                    match ts.Machine.Data.disposalStack with
                    | null -> ()
                    | _ ->
                        let mutable exn = None

                        for d in Seq.rev ts.Machine.Data.disposalStack do
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
        member ts.Current =
            match ts.hijack () with
            | Some tg -> (tg :> IAsyncEnumerator<'T>).Current
            | None ->
                match ts.Machine.Data.current with
                | ValueSome x -> x
                | ValueNone -> failwith "no current value"

        member ts.MoveNextAsync() =
            match ts.hijack () with
            | Some tg -> (tg :> IAsyncEnumerator<'T>).MoveNextAsync()
            | None ->
                if verbose then
                    printfn "MoveNextAsync..."

                if ts.Machine.ResumptionPoint = -1 then // can't use as IAsyncEnumerator before IAsyncEnumerable
                    ValueTask<bool>()
                else
                    let data = ts.Machine.Data
                    data.promiseOfValueOrEnd.Reset()
                    let mutable ts = ts
                    data.builder.MoveNext(&ts)

                    // If the move did a hijack then get the result from the final one
                    match ts.hijack () with
                    | Some tg -> tg.MoveNextAsyncResult()
                    | None -> ts.MoveNextAsyncResult()

    override ts.MoveNextAsyncResult() =
        let data = ts.Machine.Data
        let version = data.promiseOfValueOrEnd.Version
        let status = data.promiseOfValueOrEnd.GetStatus(version)

        if status = ValueTaskSourceStatus.Succeeded then
            let result = data.promiseOfValueOrEnd.GetResult(version)
            ValueTask<bool>(result)
        else
            if verbose then
                printfn "MoveNextAsync pending/faulted/cancelled..."

            ValueTask<bool>(ts, version) // uses IValueTaskSource<'T>

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

                    try
                        //printfn "at Run.MoveNext start"
                        //Console.WriteLine("[{0}] resuming by invoking {1}....", sm.MethodBuilder.Task.Id, hashq sm.ResumptionFunc )
                        let __stack_code_fin = code.Invoke(&sm)
                        //printfn $"at Run.MoveNext, __stack_code_fin={__stack_code_fin}"
                        if __stack_code_fin then
                            //printfn $"at Run.MoveNext, done"
                            sm.Data.promiseOfValueOrEnd.SetResult(false)
                            sm.Data.builder.Complete()
                        elif sm.Data.current.IsSome then
                            //printfn $"at Run.MoveNext, yield"
                            sm.Data.promiseOfValueOrEnd.SetResult(true)
                        else
                            // Goto request
                            match sm.Data.tailcallTarget with
                            | Some tg ->
                                //printfn $"at Run.MoveNext, hijack"
                                let mutable tg = tg
                                MoveNext(&tg)
                            | None ->
                                //printfn $"at Run.MoveNext, await"
                                let boxed = sm.Data.boxed

                                sm.Data.awaiter.UnsafeOnCompleted(
                                    Action(fun () ->
                                        let mutable boxed = boxed
                                        MoveNext(&boxed))
                                )

                    with exn ->
                        //Console.WriteLine("[{0}] SetException {1}", sm.MethodBuilder.Task.Id, exn)
                        sm.Data.promiseOfValueOrEnd.SetException(exn)
                        sm.Data.builder.Complete()
                //-- RESUMABLE CODE END
                ))
                (SetStateMachineMethodImpl<_>(fun sm state -> ()))
                (AfterCode<_, _>(fun sm ->
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


    member inline _.Zero() : TaskSeqCode<'T> = ResumableCode.Zero()

    member inline _.Combine(task1: TaskSeqCode<'T>, task2: TaskSeqCode<'T>) : TaskSeqCode<'T> =
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
                    printfn "Returning completed task (in while)"
                    __stack_condition_fin <- true
                    condition_res <- __stack_vtask.Result
                else
                    printfn "Awaiting non-completed task (in while)"
                    let task = __stack_vtask.AsTask()
                    let mutable awaiter = task.GetAwaiter()
                    // This will yield with __stack_fin = false
                    // This will resume with __stack_fin = true
                    let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                    __stack_condition_fin <- __stack_yield_fin

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
                    printfn "calling AwaitUnsafeOnCompleted"

                sm.Data.awaiter <- awaiter
                sm.Data.current <- ValueNone
                false)

    member inline _.Bind(task: ValueTask<'TResult1>, continuation: ('TResult1 -> TaskSeqCode<'T>)) : TaskSeqCode<'T> =
        TaskSeqCode<'T>(fun sm ->
            let mutable awaiter = task.GetAwaiter()
            let mutable __stack_fin = true

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
                    printfn "calling AwaitUnsafeOnCompleted"

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
