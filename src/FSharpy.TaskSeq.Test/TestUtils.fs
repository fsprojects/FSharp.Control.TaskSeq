namespace FSharpy.TaskSeq.Tests

open System
open System.Threading.Tasks

open FsToolkit.ErrorHandling

open FSharpy

/// Creates dummy tasks with a randomized delay and a mutable state,
/// to ensure we properly test whether processing is done ordered or not.
type DummyTaskFactory() =
    let mutable x = 0
    let rnd = Random()
    let rnd () = rnd.Next(10, 30)

    let runTask i = backgroundTask {
        // ensure unequal running lengths and points-in-time for assigning the variable
        // DO NOT use Thead.Sleep(), it's blocking!
        let! _ = Task.Delay(rnd ())
        x <- x + 1
        return x // this dereferences the variable
    }

    member _.CreateDelayedTasks total = [
        for i in 0 .. total - 1 do
            fun () -> runTask i
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

    /// Create a bunch of dummy tasks
    let createDummyTaskSeq count =
        /// Set of delayed tasks in the form of `unit -> Task<int>`
        let tasks = DummyTaskFactory().CreateDelayedTasks count

        taskSeq {
            for task in tasks do
                // cannot use `yield!` here, as `taskSeq` expects it to return a seq
                let! x = task ()
                yield x
        }
