module TaskSeq.Tests.``taskSeq forced dynamic path``

// The `taskSeq` builder has two implementations of its resumable code:
//
//  - the static one, which the F# compiler lowers into a struct state machine, and
//  - the dynamic one (`TaskSeqDynamic` / `TaskSeqDynamicInfo` and the `else` branches
//    of `if __useResumableCode`), used whenever the compiler cannot (or decides not to)
//    lower the computation expression, e.g. in FSI, or when the compiler falls back with
//    warning FS3511.
//
// In normally compiled test code, `taskSeq { .. }` and `taskSeqDynamic { .. }` are always
// lowered statically, so the dynamic path is never exercised. The builder below forces the
// dynamic path by making `Run` non-inline: the resumable code then arrives as an opaque
// delegate that was built outside of any state machine, so every `__useResumableCode`
// check inside the builder members evaluates to false.
//
// NOTE: the sequences under test are created in ordinary functions, *not* inside the `task { .. }`
// of the test itself. When dynamic resumable code is nested inside a statically compiled state
// machine, the compiler resolves `__useResumableCode` of the nested code using the outer state
// machine, which gives FS3401 errors (or invalid IL with older compilers).
//
// The important cases are those where an await does *not* complete synchronously, since
// only then the dynamic code has to suspend and resume.

#nowarn "57" // Experimental library feature, requires '--langversion:preview'.
#nowarn "3513" // Resumable code invocation: intentionally calling ResumableCode as a regular delegate.
#nowarn "1204" // TaskBuilder.RunDynamic is for use by compiled F# code.

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Xunit
open FsUnit.Xunit

open FSharp.Core.CompilerServices
open FSharp.Control

/// A taskSeq builder that always uses the dynamic (ResumptionDynamicInfo-based) implementation.
type ForcedDynamicTaskSeqBuilder() =
    inherit TaskSeqDynamicBuilder()

    // not inline: this is what prevents the compiler from lowering the CE to a state machine
    member _.Run(code: ResumableTSC<'T>) : TaskSeq<'T> =
        let ts = TaskSeqDynamic<'T>()
        ts._initialResumptionFunc <- TaskSeqResumptionFunc<'T>(fun sm -> code.Invoke(&sm))
        ts :> TaskSeq<'T>

let dynTaskSeq = ForcedDynamicTaskSeqBuilder()

/// Runs `task { for x in source do body x }` on FSharp.Core's dynamic task implementation. Used to
/// test the `for .. in taskSeq` extension of the `task` CE (see TaskExtensions.fs) on its dynamic path.
/// The CE is desugared by hand, because TaskBuilder cannot be subclassed to get a non-inline Run.
let dynTaskFor (source: TaskSeq<'T>) (body: 'T -> unit) : Task<unit> =
    TaskBuilder.RunDynamic(
        task.Delay(fun () ->
            task.For(
                source,
                fun x ->
                    body x
                    task.Zero()
            ))
    )

/// Returns a task that completes asynchronously, with the given result.
let delayed (x: 'T) = task {
    do! Task.Delay 1
    return x
}

/// A task that completes asynchronously, with an exception.
let delayedFail () : Task<int> = task {
    do! Task.Delay 1
    return raise (InvalidOperationException "boom")
}

/// An external IAsyncEnumerable (not built with taskSeq), with control over
/// whether MoveNextAsync and DisposeAsync complete synchronously or not.
type ExternalSource(items: int list, asyncMoveNext: bool, asyncDispose: bool) =
    member val DisposeCount = 0 with get, set
    member val MoveNextCount = 0 with get, set

    interface IAsyncEnumerable<int> with
        member this.GetAsyncEnumerator _ =
            let remaining = ref items
            let current = ref 0

            { new IAsyncEnumerator<int> with
                member _.Current = current.Value

                member _.MoveNextAsync() =
                    this.MoveNextCount <- this.MoveNextCount + 1

                    let hasNext =
                        match remaining.Value with
                        | x :: rest ->
                            current.Value <- x
                            remaining.Value <- rest
                            true
                        | [] -> false

                    if asyncMoveNext then
                        ValueTask<bool>(delayed hasNext)
                    else
                        ValueTask<bool> hasNext

                member _.DisposeAsync() =
                    if asyncDispose then
                        ValueTask(
                            task {
                                do! Task.Delay 1
                                this.DisposeCount <- this.DisposeCount + 1
                            }
                        )
                    else
                        this.DisposeCount <- this.DisposeCount + 1
                        ValueTask()
            }

//
// The sequences under test. Created outside of any statically compiled state machine, see the note at the top.
//

module Seqs =
    let simple () = dynTaskSeq {
        yield 1
        yield 2
        yield 3
    }

    let empty () : TaskSeq<int> = dynTaskSeq { () }

    let awaitTaskBetweenYields () = dynTaskSeq {
        yield 1
        do! Task.Delay 1
        yield 2
        do! Task.Delay 1
        yield 3
    }

    // YieldAwaitable goes through the SRTP ^TaskLike Bind overload
    let awaitTaskYieldBetweenYields () = dynTaskSeq {
        yield 1
        do! Task.Yield()
        yield 2
        do! Task.Yield()
        yield 3
    }

    let letBangTask () = dynTaskSeq {
        let! x = delayed 10
        yield x
        let! y = delayed 20
        yield x + y
    }

    let letBangValueTask () = dynTaskSeq {
        let! x = ValueTask<int>(delayed 10)
        yield x
        let! y = ValueTask<int>(delayed 20)
        yield x + y
    }

    let letBangAsync () = dynTaskSeq {
        let! x = async {
            do! Async.Sleep 1
            return 10
        }

        yield x

        let! y = async {
            do! Async.Sleep 1
            return 20
        }

        yield x + y
    }

    let awaitsInLoops () = dynTaskSeq {
        for i in 1..3 do
            do! Task.Delay 1
            yield i

        let mutable i = 4

        while i <= 6 do
            let! x = delayed i
            yield x
            i <- i + 1
    }

    let awaitFaultedTask () = dynTaskSeq {
        yield 1
        let! x = delayedFail ()
        yield x
    }

    let tryWithAfterAwait () = dynTaskSeq {
        try
            yield 1
            do! Task.Delay 1
            raise (InvalidOperationException "boom")
            yield 2
        with :? InvalidOperationException ->
            yield 99
    }

    let tryFinallyAfterAwait (finallyCount: int ref) = dynTaskSeq {
        try
            yield 1
            do! Task.Delay 1
            yield 2
        finally
            finallyCount.Value <- finallyCount.Value + 1
    }

    let forOverAsyncEnumerable (source: IAsyncEnumerable<int>) = dynTaskSeq {
        for x in source do
            yield x
    }

    let awaitingInner () = taskSeq {
        for i in 1..5 do
            do! Task.Delay 1
            yield i
    }

    let yieldBangAwaitingInner () =
        let inner = dynTaskSeq {
            do! Task.Delay 1
            yield 1
            do! Task.Delay 1
            yield 2
        }

        dynTaskSeq {
            yield! inner
            yield 3
            yield! inner
        }

    let earlyDisposal (finallyCount: int ref) = dynTaskSeq {
        try
            for i in 1..10 do
                do! Task.Delay 1
                yield i
        finally
            finallyCount.Value <- finallyCount.Value + 1
    }

    let reEnumerate () = dynTaskSeq {
        for i in 1..3 do
            let! x = delayed i
            yield x
    }

    let largeSyncLoop () = dynTaskSeq {
        for i in 1..10_000 do
            if i % 1000 = 0 then
                yield i
    }

[<Fact>]
let ``CE forced-dynamic builder really uses the dynamic path`` () =
    Seqs.simple ()
    |> should be instanceOfType<TaskSeqDynamic<int>>

[<Fact>]
let ``CE forced-dynamic empty and simple yields`` () = task {
    let! empty = Seqs.empty () |> TaskSeq.toListAsync
    empty |> should be Empty

    let! data = Seqs.simple () |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3 ]
}

[<Fact>]
let ``CE forced-dynamic do! Task between yields`` () = task {
    let! data = Seqs.awaitTaskBetweenYields () |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3 ]
}

[<Fact>]
let ``CE forced-dynamic do! Task.Yield between yields`` () = task {
    let! data = Seqs.awaitTaskYieldBetweenYields () |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3 ]
}

[<Fact>]
let ``CE forced-dynamic let! Task<'T>`` () = task {
    let! data = Seqs.letBangTask () |> TaskSeq.toListAsync
    data |> should equal [ 10; 30 ]
}

[<Fact>]
let ``CE forced-dynamic let! ValueTask<'T>`` () = task {
    let! data = Seqs.letBangValueTask () |> TaskSeq.toListAsync
    data |> should equal [ 10; 30 ]
}

[<Fact>]
let ``CE forced-dynamic let! Async<'T>`` () = task {
    let! data = Seqs.letBangAsync () |> TaskSeq.toListAsync
    data |> should equal [ 10; 30 ]
}

[<Fact>]
let ``CE forced-dynamic awaits inside for and while loops`` () = task {
    let! data = Seqs.awaitsInLoops () |> TaskSeq.toListAsync
    data |> should equal [ 1..6 ]
}

[<Fact>]
let ``CE forced-dynamic exception from awaited faulted task propagates`` () = task {
    let! exn = Assert.ThrowsAsync<InvalidOperationException>(fun () -> Seqs.awaitFaultedTask () |> TaskSeq.toListAsync :> Task)
    exn.Message |> should equal "boom"
}

[<Fact>]
let ``CE forced-dynamic try-with catches exception raised after an await`` () = task {
    let! data = Seqs.tryWithAfterAwait () |> TaskSeq.toListAsync
    data |> should equal [ 1; 99 ]
}

[<Fact>]
let ``CE forced-dynamic try-finally runs finally after awaits`` () = task {
    let finallyCount = ref 0

    let! data =
        Seqs.tryFinallyAfterAwait finallyCount
        |> TaskSeq.toListAsync

    data |> should equal [ 1; 2 ]
    finallyCount.Value |> should equal 1
}

[<Fact>]
let ``CE forced-dynamic for over IAsyncEnumerable with async MoveNextAsync`` () = task {
    let source = ExternalSource([ 1..5 ], asyncMoveNext = true, asyncDispose = false)
    let! data = Seqs.forOverAsyncEnumerable source |> TaskSeq.toListAsync
    data |> should equal [ 1..5 ]
    source.MoveNextCount |> should equal 6
    source.DisposeCount |> should equal 1
}

[<Theory; InlineData false; InlineData true>]
let ``CE forced-dynamic for over IAsyncEnumerable with async DisposeAsync does not repeat the last item`` asyncMoveNext = task {
    let source = ExternalSource([ 1..3 ], asyncMoveNext, asyncDispose = true)
    let! data = Seqs.forOverAsyncEnumerable source |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3 ]
    source.DisposeCount |> should equal 1
}

[<Fact>]
let ``CE forced-dynamic for over awaiting taskSeq`` () = task {
    let! data =
        Seqs.forOverAsyncEnumerable (Seqs.awaitingInner ())
        |> TaskSeq.toListAsync

    data |> should equal [ 1..5 ]
}

[<Fact>]
let ``CE forced-dynamic yield! awaiting taskSeq`` () = task {
    let! data = Seqs.yieldBangAwaitingInner () |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3; 1; 2 ]
}

[<Fact>]
let ``CE forced-dynamic early disposal runs pending finally`` () = task {
    let finallyCount = ref 0

    let! data =
        Seqs.earlyDisposal finallyCount
        |> TaskSeq.take 2
        |> TaskSeq.toListAsync

    data |> should equal [ 1; 2 ]
    finallyCount.Value |> should equal 1
}

[<Fact>]
let ``CE forced-dynamic re-enumeration with awaits`` () = task {
    let ts = Seqs.reEnumerate ()
    let! data1 = ts |> TaskSeq.toListAsync
    let! data2 = ts |> TaskSeq.toListAsync
    data1 |> should equal [ 1; 2; 3 ]
    data2 |> should equal [ 1; 2; 3 ]
}

[<Fact>]
let ``CE forced-dynamic large synchronous loop`` () = task {
    let! data = Seqs.largeSyncLoop () |> TaskSeq.toListAsync
    data |> should equal [ 1000..1000..10_000 ]
}

[<Fact>]
let ``task CE forced-dynamic for over IAsyncEnumerable with async MoveNextAsync`` () = task {
    let source = ExternalSource([ 1..5 ], asyncMoveNext = true, asyncDispose = false)
    let items = ResizeArray()
    do! dynTaskFor source items.Add
    List.ofSeq items |> should equal [ 1..5 ]
    source.DisposeCount |> should equal 1
}

[<Fact>]
let ``task CE forced-dynamic for over awaiting taskSeq`` () = task {
    let items = ResizeArray()
    do! dynTaskFor (Seqs.awaitingInner ()) items.Add
    List.ofSeq items |> should equal [ 1..5 ]
}

// Same scenario on the static path: when the source's DisposeAsync completes asynchronously, the
// TryFinallyAsync compensation suspends while `current` still holds the last yielded item.
// See https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/452
[<Theory; InlineData false; InlineData true>]
let ``CE static for over IAsyncEnumerable with async DisposeAsync does not repeat the last item`` asyncMoveNext = task {
    let source = ExternalSource([ 1..3 ], asyncMoveNext, asyncDispose = true)

    let ts = taskSeq {
        for x in source do
            yield x
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3 ]
    source.DisposeCount |> should equal 1
}

[<Theory; InlineData false; InlineData true>]
let ``TaskSeq.map over IAsyncEnumerable with async DisposeAsync does not repeat the last item`` asyncMoveNext = task {
    let source = ExternalSource([ 1..3 ], asyncMoveNext, asyncDispose = true)

    let! data =
        source
        |> TaskSeq.map (fun x -> x * 10)
        |> TaskSeq.toListAsync

    data |> should equal [ 10; 20; 30 ]
    source.DisposeCount |> should equal 1
}
