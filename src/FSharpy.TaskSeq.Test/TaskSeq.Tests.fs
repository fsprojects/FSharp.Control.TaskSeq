module Tests

open System

open System.Threading.Tasks

open FsToolkit.ErrorHandling
open FsUnit.Xunit
open FSharpy
open Xunit

[<AutoOpen>]
module Internal =
    /// Creates dummy tasks with a randomized delay and a mutable state,
    /// to ensure we properly test whether processing is done ordered or not.
    type TaskFactory() =
        let mutable x = 0
        let rnd = Random()
        let rnd () = rnd.Next(10, 30)

        let runTask i = backgroundTask {
            // ensure unequal running lengths and points-in-time for assigning the variable
            let! _ = Task.Delay(rnd ())
            x <- x + 1
            return x // this dereferences the variable
        }

        member _.createBunchOfTasks total = [
            for i in 0 .. total - 1 do
                fun () -> runTask i
        ]

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
        let tasks = TaskFactory().createBunchOfTasks total
        let combinedTask = joiner tasks
        // start the combined tasks
        combinedTask ()

module TaskSeqTests =
    let createAsyncEnum count =
        let tasks = TaskFactory().createBunchOfTasks count

        taskSeq {
            for task in tasks do
                // cannot use `yield!` here, as `taskSeq` expects it to return a seq
                let! x = task ()
                yield x
        }

    [<Fact>]
    let ``TaskSeq-iteriAsync should go over all items`` () = task {
        let tq = createAsyncEnum 10
        let mutable sum = 0
        do! tq |> TaskSeq.iteriAsync (fun i _ -> sum <- sum + i)
        sum |> should equal 45 // index starts at 0
    }

    [<Fact>]
    let ``TaskSeq-iterAsync should go over all items`` () = task {
        let tq = createAsyncEnum 10
        let mutable sum = 0
        do! tq |> TaskSeq.iterAsync (fun item -> sum <- sum + item)
        sum |> should equal 55 // task-dummies started at 1
    }

    [<Fact>]
    let ``TaskSeq-map maps in correct order`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.map (fun item -> char (item + 64))
            |> TaskSeq.toSeqCachedAsync

        sq
        |> Seq.map string
        |> String.concat ""
        |> should equal "ABCDEFGHIJ"
    }

    [<Fact>]
    let ``TaskSeq-mapAsync maps in correct order`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.mapAsync (fun item -> task { return char (item + 64) })
            |> TaskSeq.toSeqCachedAsync

        sq
        |> Seq.map string
        |> String.concat ""
        |> should equal "ABCDEFGHIJ"
    }

    [<Fact>]
    let ``TaskSeq-collect operates in correct order`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.collect (fun item -> taskSeq {
                yield char (item + 64)
                yield char (item + 65)
            })
            |> TaskSeq.toSeqCachedAsync

        sq
        |> Seq.map string
        |> String.concat ""
        |> should equal "ABBCCDDEEFFGGHHIIJJK"
    }

    [<Fact>]
    let ``TaskSeq-collectSeq operates in correct order`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.collectSeq (fun item -> seq {
                yield char (item + 64)
                yield char (item + 65)
            })
            |> TaskSeq.toSeqCachedAsync

        sq
        |> Seq.map string
        |> String.concat ""
        |> should equal "ABBCCDDEEFFGGHHIIJJK"
    }

    [<Fact>]
    let ``TaskSeq-collect with empty task sequences`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.collect (fun _ -> TaskSeq.ofSeq Seq.empty)
            |> TaskSeq.toSeqCachedAsync

        Seq.isEmpty sq |> should be True
    }

    [<Fact>]
    let ``TaskSeq-collectSeq with empty sequences`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.collectSeq (fun _ -> Seq.empty<int>)
            |> TaskSeq.toSeqCachedAsync

        Seq.isEmpty sq |> should be True
    }

    [<Fact>]
    let ``TaskSeq-empty is empty`` () = task {
        let! sq = TaskSeq.empty<string> |> TaskSeq.toSeqCachedAsync
        Seq.isEmpty sq |> should be True
    }


module ``PoC's for seq of tasks`` =


    [<Fact>]
    let ``Good: Show joining tasks with continuation is good`` () = task {
        // acts like a fold
        let! results = createAndJoinMultipleTasks 10 joinWithContinuation
        results |> should equal 10
    }

    [<Fact>]
    let ``Good: Show that joining tasks with 'bind' in task CE is good`` () = task {
        let! tasks = createAndJoinMultipleTasks 10 joinIdentityDelayed

        let tasks = tasks |> Array.ofList
        let len = Array.length tasks
        let results = Array.zeroCreate len

        for i in 0 .. len - 1 do
            // this uses Task.bind under the hood, which ensures order-of-execution and wait-for-previous
            let! item = tasks[i]() // only now are we delay-executing the task in the array
            results[i] <- item

        results |> should equal <| Array.init len ((+) 1)
    }

    [<Fact>]
    let ``Good: Show that joining tasks with 'taskSeq' is good`` () = task {
        let! tasks = createAndJoinMultipleTasks 10 joinIdentityDelayed

        let asAsyncSeq = taskSeq {
            for task in tasks do
                // cannot use `yield!` here, as `taskSeq` expects it to return a seq
                let! x = task ()
                yield x
        }

        let! results = asAsyncSeq |> TaskSeq.toArrayAsync

        results |> should equal
        <| Array.init (Array.length results) ((+) 1)
    }

    [<Fact>]
    let ``Bad: Show that joining tasks with 'traverseTaskResult' can be bad`` () = task {
        let! taskList = createAndJoinMultipleTasks 10 joinIdentityHotStarted

        // since tasks are hot-started, by this time they are already *all* running
        let! results =
            taskList
            |> List.map (Task.map Result<int, string>.Ok)
            |> List.traverseTaskResultA id

        match results with
        | Ok results ->
            results |> should not'
            <| equal (List.init (List.length results) ((+) 1))
        | Error err -> failwith $"Impossible: {err}"
    }

    [<Fact>]
    let ``Bad: Show that joining tasks as a list of tasks can be bad`` () = task {
        let! taskList = createAndJoinMultipleTasks 10 joinIdentityHotStarted

        // since tasks are hot-started, by this time they are already *all* running
        let tasks = taskList |> Array.ofList
        let results = Array.zeroCreate 10

        for i in 0..9 do
            let! item = tasks[i]
            results[i] <- item

        results |> should not'
        <| equal (Array.init (Array.length results) ((+) 1))
    }
