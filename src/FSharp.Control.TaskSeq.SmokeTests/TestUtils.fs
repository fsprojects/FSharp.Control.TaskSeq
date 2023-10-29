namespace TaskSeq.Tests

open System
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open System.Collections.Generic

open Xunit
open Xunit.Abstractions
open FsUnit.Xunit

open FSharp.Control

/// Milliseconds
[<Measure>]
type ms

/// Microseconds
[<Measure>]
type µs

/// Helpers for short waits, as Task.Delay has about 15ms precision.
/// Inspired by IoT code: https://github.com/dotnet/iot/pull/235/files
module DelayHelper =

    let private rnd = Random()

    /// <summary>
    /// Delay for at least the specified <paramref name="microseconds"/>.
    /// </summary>
    /// <param name="microseconds">The number of microseconds to delay.</param>
    /// <param name="allowThreadYield">
    /// True to allow yielding the thread. If this is set to false, on single-proc systems
    /// this will prevent all other code from running.
    /// </param>
    let spinWaitDelay (microseconds: int64<µs>) (allowThreadYield: bool) =
        let start = Stopwatch.GetTimestamp()
        let minimumTicks = int64 microseconds * Stopwatch.Frequency / 1_000_000L

        // FIXME: though this is part of official IoT code, the `allowThreadYield` version is extremely slow
        // slower than would be expected from a simple SpinOnce. Though this may be caused by scenarios with
        // many tasks at once. Have to investigate. See perf smoke tests.
        if allowThreadYield then
            let spinWait = SpinWait()

            while Stopwatch.GetTimestamp() - start < minimumTicks do
                spinWait.SpinOnce(1)

        else
            while Stopwatch.GetTimestamp() - start < minimumTicks do
                Thread.SpinWait(1)

    let delayTask (µsecMin: int64<µs>) (µsecMax: int64<µs>) f = task {
        let rnd () = rnd.NextInt64(int64 µsecMin, int64 µsecMax) * 1L<µs>

        // ensure unequal running lengths and points-in-time for assigning the variable
        // DO NOT use Thead.Sleep(), it's blocking!
        // WARNING: Task.Delay only has a 15ms timer resolution!!!

        // TODO: check this! The following comment may not be correct
        // this creates a resume state, which seems more efficient than SpinWait.SpinOnce, see DelayHelper.
        let! _ = Task.Delay 0
        let delay = rnd ()

        // typical minimum accuracy of Task.Delay is 15.6ms
        // for delay-cases shorter than that, we use SpinWait
        if delay < 15_000L<µs> then
            do spinWaitDelay (rnd ()) false
        else
            do! Task.Delay(int <| float delay / 1_000.0)

        return f ()
    }

/// <summary>
/// Creates dummy backgroundTasks with a randomized delay and a mutable state,
/// to ensure we properly test whether processing is done ordered or not.
/// Default for <paramref name="µsecMin" /> and <paramref name="µsecMax" />
/// are 10,000µs and 30,000µs respectively (or 10ms and 30ms).
/// </summary>
type DummyTaskFactory(µsecMin: int64<µs>, µsecMax: int64<µs>) =
    let mutable x = 0

    /// <summary>
    /// Creates dummy tasks with a randomized delay and a mutable state,
    /// to ensure we properly test whether processing is done ordered or not.
    /// Uses the defaults for <paramref name="µsecMin" /> and <paramref name="µsecMax" />
    /// with 10,000µs and 30,000µs respectively (or 10ms and 30ms).
    /// </summary>
    new() = new DummyTaskFactory(10_000L<µs>, 30_000L<µs>)


    /// Bunch of delayed tasks that randomly have a yielding delay of 10-30ms, therefore having overlapping execution times.
    member _.CreateDelayedTasks_SideEffect total = [
        for i in 0 .. total - 1 do
            fun () -> DelayHelper.delayTask µsecMin µsecMax (fun _ -> Interlocked.Increment &x)
    ]

/// Just some dummy task generators, copied over from the base test project, with artificial delays,
/// mostly to ensure sequential async operation of side effects.
module Gen =
    /// Joins two tasks using merely BCL methods. This approach is what you can use to
    /// properly, sequentially execute a chain of tasks in a non-blocking, non-overlapping way.
    let joinWithContinuation tasks =
        let simple (t: unit -> Task<_>) (source: unit -> Task<_>) : unit -> Task<_> =
            fun () ->
                source()
                    .ContinueWith((fun (_: Task) -> t ()), TaskContinuationOptions.OnlyOnRanToCompletion)
                    .Unwrap()
                :?> Task<_>

        let rec combine acc (tasks: (unit -> Task<_>) list) =
            match tasks with
            | [] -> acc
            | t :: tail -> combine (simple t acc) tail

        match tasks with
        | first :: rest -> combine first rest
        | [] -> failwith "oh oh, no tasks given!"

    let joinIdentityHotStarted tasks () = task { return tasks |> List.map (fun t -> t ()) }

    let joinIdentityDelayed tasks () = task { return tasks }

    let createAndJoinMultipleTasks total joiner : Task<_> =
        // the actual creation of tasks
        let tasks = DummyTaskFactory().CreateDelayedTasks_SideEffect total
        let combinedTask = joiner tasks
        // start the combined tasks
        combinedTask ()
