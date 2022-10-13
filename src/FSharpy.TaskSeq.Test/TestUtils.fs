namespace FSharpy.TaskSeq.Tests

open System
open System.Threading
open System.Threading.Tasks
open System.Diagnostics

open FsToolkit.ErrorHandling

open FSharpy

/// Milliseconds
[<Measure>]
type ms

/// Microseconds
[<Measure>]
type µs

/// Helpers for short waits, as Task.Delay has about 15ms precision.
/// Inspired by IoT code: https://github.com/dotnet/iot/pull/235/files
module DelayHelper =

    /// <summary>
    /// Delay for at least the specified <paramref name="microseconds"/>.
    /// </summary>
    /// <param name="microseconds">The number of microseconds to delay.</param>
    /// <param name="allowThreadYield">
    /// True to allow yielding the thread. If this is set to false, on single-proc systems
    /// this will prevent all other code from running.
    /// </param>
    let delayMicroseconds microseconds (allowThreadYield: bool) =
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


/// <summary>
/// Creates dummy tasks with a randomized delay and a mutable state,
/// to ensure we properly test whether processing is done ordered or not.
/// Default for <paramref name="µsecMin" /> and <paramref name="µsecMax" />
/// are 10,000µs and 30,000µs respectively (or 10ms and 30ms).
/// </summary>
type DummyTaskFactory(µsecMin: int64<µs>, µsecMax: int64<µs>) =
    let mutable x = 0
    let rnd = Random()
    let rnd () = rnd.NextInt64(int64 µsecMin, int64 µsecMax) * 1L<µs>

    let runTaskDelayed i = backgroundTask {
        // ensure unequal running lengths and points-in-time for assigning the variable
        // DO NOT use Thead.Sleep(), it's blocking!
        // WARNING: Task.Delay only has a 15ms timer resolution!!!
        //let! _ = Task.Delay(rnd ())
        let! _ = Task.Delay 0 // this creates a resume state, which seems more efficient than SpinWait.SpinOnce, see DelayHelper.
        DelayHelper.delayMicroseconds (rnd ()) false
        x <- x + 1
        return x // this dereferences the variable
    }

    let runTaskDirect i = backgroundTask {
        x <- x + 1
        return x
    }


    /// <summary>
    /// Creates dummy tasks with a randomized delay and a mutable state,
    /// to ensure we properly test whether processing is done ordered or not.
    /// Uses the defaults for <paramref name="µsecMin" /> and <paramref name="µsecMax" />
    /// with 10,000µs and 30,000µs respectively (or 10ms and 30ms).
    /// </summary>
    new() = new DummyTaskFactory(10_000L<µs>, 30_000L<µs>)

    /// <summary>
    /// Creates dummy tasks with a randomized delay and a mutable state,
    /// to ensure we properly test whether processing is done ordered or not.
    /// Values <paramref name="msecMin" /> and <paramref name="msecMax" /> can be
    /// given in milliseconds.
    /// </summary>
    new(msecMin: int<ms>, msecMax: int<ms>) = new DummyTaskFactory(int64 msecMin * 1000L<µs>, int64 msecMax * 1000L<µs>)


    /// Bunch of delayed tasks that randomly have a yielding delay of 10-30ms, therefore having overlapping execution times.
    member _.CreateDelayedTasks total = [
        for i in 0 .. total - 1 do
            fun () -> runTaskDelayed i
    ]

    /// Bunch of delayed tasks without internally using Task.Delay, therefore hot-started and immediately finished.
    member _.CreateDirectTasks total = [
        for i in 0 .. total - 1 do
            fun () -> runTaskDirect i
    ]

[<AutoOpen>]
module TestUtils =

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
        let tasks = DummyTaskFactory().CreateDelayedTasks total
        let combinedTask = joiner tasks
        // start the combined tasks
        combinedTask ()

    /// Create a bunch of dummy tasks, with varying microsecond delays.
    let createDummyTaskSeqWith (min: int64<µs>) max count =
        /// Set of delayed tasks in the form of `unit -> Task<int>`
        let tasks = DummyTaskFactory(min, max).CreateDelayedTasks count

        taskSeq {
            for task in tasks do
                // cannot use `yield!` here, as `taskSeq` expects it to return a seq
                let! x = task ()
                yield x
        }

    /// Create a bunch of dummy tasks, which are sequentially hot-started, WITHOUT artificial spin-wait delays.
    let createDummyDirectTaskSeq count =
        /// Set of delayed tasks in the form of `unit -> Task<int>`
        let tasks = DummyTaskFactory().CreateDirectTasks count

        taskSeq {
            for task in tasks do
                // cannot use `yield!` here, as `taskSeq` expects it to return a seq
                let! x = task ()
                yield x
        }

    /// Create a bunch of dummy tasks, each lasting between 10-30ms with spin-wait delays.
    let createDummyTaskSeq = createDummyTaskSeqWith 10_0000L<µs> 30_0000L<µs>
