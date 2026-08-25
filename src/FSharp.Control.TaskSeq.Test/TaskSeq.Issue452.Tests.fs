module TaskSeq.Tests.``Issue 452 -- external IAsyncEnumerable on custom scheduler``

// See https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/452
//
// Report: wrapping combinators (TaskSeq.map, `taskSeq { for .. in .. do yield .. }`) over an
// externally-produced IAsyncEnumerable<'T> allegedly yield the *final item twice* when the
// consuming code runs on a non-default TaskScheduler (observed inside a Microsoft Orleans grain
// activation, which runs on a custom per-activation TaskScheduler).
//
// Investigation: several attempts to reproduce this offline -- using a custom single-threaded
// TaskScheduler, a custom SynchronizationContext, batched/async MoveNextAsync implementations
// with artificial delays, and both immediate and Task.Run-based completions -- did not reproduce
// the duplication. The reporter also could not reduce it to a standalone console repro and
// reports it only reproduces under the specific Orleans per-activation scheduler.
//
// These tests capture the discriminating shape from the report (an external, hand-written
// IAsyncEnumerable<'T> wrapped by TaskSeq.map and by `taskSeq { for .. do yield .. }`, consumed
// while running on a non-default TaskScheduler) as a regression guard. They currently pass; if
// the underlying bug is ever reproduced and fixed, they should continue to pass and can absorb
// a more targeted assertion at that time.

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Xunit
open FsUnit.Xunit

open FSharp.Control

/// A minimal externally-produced IAsyncEnumerable, similar in shape to what
/// Orleans' IAsyncEnumerableGrainExtension produces: MoveNextAsync sometimes
/// completes asynchronously (via Task.Run), mimicking batched pulls over
/// grain calls.
type private ExternalEnumerator(itemCount: int) =
    let mutable current = 0

    interface IAsyncEnumerator<int> with
        member _.Current = current

        member _.MoveNextAsync() =
            current <- current + 1

            if current <= itemCount then
                ValueTask<bool>(Task.Run(fun () -> true))
            else
                ValueTask<bool>(Task.Run(fun () -> false))

        member _.DisposeAsync() = ValueTask()

type private ExternalEnumerable(itemCount: int) =
    interface IAsyncEnumerable<int> with
        member _.GetAsyncEnumerator(_ct) = new ExternalEnumerator(itemCount) :> IAsyncEnumerator<int>

/// A minimal single-threaded TaskScheduler, used to emulate running inside a
/// non-default-scheduler host (like an Orleans grain activation).
type private SingleThreadTaskScheduler() =
    inherit TaskScheduler()

    let queue = new System.Collections.Concurrent.BlockingCollection<Task>()
    let mutable self = Unchecked.defaultof<SingleThreadTaskScheduler>

    let thread =
        Thread(fun () ->
            for t in queue.GetConsumingEnumerable() do
                self.RunInline t)

    do
        thread.IsBackground <- true
        thread.Start()

    member internal _.SetSelf(s) = self <- s
    member internal this.RunInline(t: Task) = this.TryExecuteTask t |> ignore
    override _.GetScheduledTasks() = Seq.empty
    override _.QueueTask(t) = queue.Add t
    override _.TryExecuteTaskInline(_t, _wasQueued) = false

let private runOnCustomScheduler (f: unit -> Task<'a>) : 'a =
    let scheduler = SingleThreadTaskScheduler()
    scheduler.SetSelf scheduler

    let t = Task.Factory.StartNew((fun () -> f ()), CancellationToken.None, TaskCreationOptions.None, scheduler).Unwrap()

    t.GetAwaiter().GetResult()

[<Fact>]
let ``TaskSeq.map over external IAsyncEnumerable on custom TaskScheduler does not duplicate last item`` () =
    let itemCount = 3

    let result =
        runOnCustomScheduler (fun () -> task {
            let upstream = ExternalEnumerable(itemCount) :> IAsyncEnumerable<int>
            let mapped = upstream |> TaskSeq.map (fun x -> x * 10)
            return! mapped |> TaskSeq.toListAsync
        })

    result |> should equal [ 10; 20; 30 ]
    result |> List.length |> should equal itemCount

[<Fact>]
let ``taskSeq { for .. in .. do yield } over external IAsyncEnumerable on custom TaskScheduler does not duplicate last item`` () =
    let itemCount = 3

    let result =
        runOnCustomScheduler (fun () -> task {
            let upstream = ExternalEnumerable(itemCount) :> IAsyncEnumerable<int>

            let wrapped = taskSeq {
                for x in upstream do
                    yield x * 10
            }

            return! wrapped |> TaskSeq.toListAsync
        })

    result |> should equal [ 10; 20; 30 ]
    result |> List.length |> should equal itemCount
