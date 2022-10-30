namespace FSharpy.Tests

open System
open System.Threading
open System.Threading.Tasks
open System.Diagnostics

open FsToolkit.ErrorHandling

open FSharpy
open System.Collections.Generic
open FsUnit.Xunit
open Xunit
open Xunit.Abstractions
open System.Runtime.Serialization
open System.Reflection
open Microsoft.FSharp.Reflection

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
        let rnd = Random()
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
    let rnd = Random()
    let rnd () = rnd.NextInt64(int64 µsecMin, int64 µsecMax) * 1L<µs>

    let runTaskDelayed () = backgroundTask {
        return! DelayHelper.delayTask µsecMin µsecMax (fun _ -> Interlocked.Increment &x)
    }

    let runTaskDirect () = backgroundTask {
        Interlocked.Increment &x |> ignore
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
            fun () -> runTaskDelayed ()
    ]

    /// Bunch of delayed tasks without internally using Task.Delay, therefore hot-started and immediately finished.
    member _.CreateDirectTasks total = [
        for i in 0 .. total - 1 do
            fun () -> runTaskDirect ()
    ]

[<AutoOpen>]
module TestUtils =
    /// Delays (no spin-wait!) between 20 and 70ms, assuming a 15.6ms resolution clock
    let longDelay () = task { do! Task.Delay(Random().Next(20, 70)) }

    /// Spin-waits, occasionally normal delay, between 50µs - 18,000µs
    let microDelay () = task { do! DelayHelper.delayTask 50L<µs> 18_000L<µs> (fun _ -> ()) }

    /// Call MoveNextAsync() and check if return value is the expected value
    let moveNextAndCheck expected (enumerator: IAsyncEnumerator<_>) = task {
        let! (hasNext: bool) = enumerator.MoveNextAsync()

        if expected then
            hasNext |> should be True
        else
            hasNext |> should be False
    }

    /// Call MoveNextAsync() and check if Current has the expected value. Uses untyped 'should equal'
    let moveNextAndCheckCurrent successMoveNext expectedValue (enumerator: IAsyncEnumerator<_>) = task {
        let! (hasNext: bool) = enumerator.MoveNextAsync()

        if successMoveNext then
            hasNext |> should be True
        else
            hasNext |> should be False

        enumerator.Current |> should equal expectedValue
    }

    /// Call MoveNext() and check if Current has the expected value. Uses untyped 'should equal'
    let seqMoveNextAndCheckCurrent successMoveNext expectedValue (enumerator: IEnumerator<_>) =
        let (hasNext: bool) = enumerator.MoveNext()

        if successMoveNext then
            hasNext |> should be True
        else
            hasNext |> should be False

        enumerator.Current |> should equal expectedValue

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

    /// Create a bunch of dummy tasks, with varying millisecond delays.
    let createLongerDummyTaskSeq (min: int<ms>) max count =
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
        // Set of non-delayed tasks in the form of `unit -> Task<int>`
        let tasks = DummyTaskFactory().CreateDirectTasks count

        taskSeq {
            for task in tasks do
                // cannot use `yield!` here, as `taskSeq` expects it to return a seq
                let! x = task ()
                yield x
        }

    /// Create a bunch of dummy tasks, each lasting between 10-30ms with spin-wait delays.
    let createDummyTaskSeq = createDummyTaskSeqWith 10_000L<µs> 30_000L<µs>

    // TODO: figure out a way to make IXunitSerializable work with DU types
    type CustomSerializable<'T>(value: 'T) =
        let mutable _value: 'T = value

        new() = CustomSerializable(Unchecked.defaultof<'T>)

        member this.Value = _value

        interface IXunitSerializable with
            member this.Deserialize info = _value <- info.GetValue<'T>("Value")

            member this.Serialize info = info.AddValue("Value", _value)

        override this.ToString() = "Value = " + string _value

    // NOTE: using enum instead of DU because *even if* we use CustomSerializable above, the
    // VS test runner will hang, and NCrunch will (properly) show that the type does not implement
    // a default constructor. See https://github.com/xunit/xunit/issues/429, amongst others.
    type EmptyVariant =
        | CallEmpty = 0
        | Do = 1
        | DoBang = 2
        | YieldBang = 3
        | YieldBangNested = 4
        | DelayDoBang = 6
        | DelayYieldBang = 7
        | DelayYieldBangNested = 8

    type SmallVariant =
        | NoThreadYield = 1
        | ThreadSpinWait = 2
        | ThreadYielded = 3
        | SideEffect_NoThreadYield = 4
        | SideEffect_ThreadSpinWait = 5
        | SideEffect_ThreadYield = 6

    /// Returns any of a set of variants that each create an empty sequence in a creative way.
    /// Please extend this with more cases.
    let getEmptyVariant variant : IAsyncEnumerable<int> =
        match variant with
        | EmptyVariant.CallEmpty -> TaskSeq.empty
        | EmptyVariant.Do -> taskSeq { do ignore () }
        | EmptyVariant.DoBang -> taskSeq { do! task { return () } } // TODO: this doesn't work with Task, only Task<unit>...
        | EmptyVariant.YieldBang -> taskSeq { yield! Seq.empty<int> }
        | EmptyVariant.YieldBangNested -> taskSeq { yield! taskSeq { do ignore () } }
        | EmptyVariant.DelayDoBang -> taskSeq {
            do! longDelay ()
            do! longDelay ()
            do! longDelay ()
          }
        | EmptyVariant.DelayYieldBang -> taskSeq {
            do! microDelay ()
            yield! Seq.empty<int>
            do! longDelay ()
            yield! Seq.empty<int>
            do! microDelay ()
          }

        | EmptyVariant.DelayYieldBangNested -> taskSeq {
            yield! taskSeq {
                do! microDelay ()
                yield! taskSeq { do! microDelay () }
                do! microDelay ()
            }

            yield! TaskSeq.empty

            yield! taskSeq {
                do! microDelay ()
                yield! taskSeq { do! microDelay () }
                do! microDelay ()
            }
          }
        | x -> failwithf "Invalid test variant: %A" x

    /// Returns a small TaskSeq of 1..10
    let getSmallVariant variant : IAsyncEnumerable<int> =
        match variant with
        | SmallVariant.NoThreadYield -> taskSeq { yield! [ 1..10 ] }
        | SmallVariant.ThreadSpinWait -> taskSeq {
            for i in 0..9 do
                let! x = DelayHelper.delayTask 50L<µs> 5_000L<µs> (fun _ -> i)
                yield x
          }

        | SmallVariant.ThreadYielded -> taskSeq {
            for i in 0..9 do
                let! x = DelayHelper.delayTask 50L<µs> 5_000L<µs> (fun _ -> i)
                yield x
          }

        | SmallVariant.SideEffect_NoThreadYield ->
            let mutable i = 0

            taskSeq {
                // F# BUG? coloring disappears?
                for x = 0 to 9 do
                    i <- i + 1
                    yield i
            }
        | SmallVariant.SideEffect_ThreadSpinWait -> createDummyTaskSeqWith 50L<µs> 5_000L<µs> 10
        | SmallVariant.SideEffect_ThreadYield -> createDummyTaskSeqWith 15_000L<µs> 50_000L<µs> 10
        | x -> failwithf "Invalid test variant: %A" x

    type TestEmptyVariants() as this =
        inherit TheoryData<EmptyVariant>()

        do
            this.Add EmptyVariant.CallEmpty
            this.Add EmptyVariant.Do
            this.Add EmptyVariant.DoBang
            this.Add EmptyVariant.YieldBang
            this.Add EmptyVariant.YieldBangNested
            this.Add EmptyVariant.DelayDoBang
            this.Add EmptyVariant.DelayYieldBang
            this.Add EmptyVariant.DelayYieldBangNested

    type TestSmallVariants() as this =
        inherit TheoryData<SmallVariant>()

        do
            this.Add SmallVariant.NoThreadYield
            this.Add SmallVariant.ThreadSpinWait
            this.Add SmallVariant.ThreadYielded
            this.Add SmallVariant.SideEffect_NoThreadYield
            this.Add SmallVariant.SideEffect_ThreadSpinWait
            this.Add SmallVariant.SideEffect_ThreadYield
