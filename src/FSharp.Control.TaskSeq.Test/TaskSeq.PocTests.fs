namespace TaskSeq.Tests

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

/////////////////////////////////////////////////////////////////////////////
///                                                                       ///
/// This file contains bunch of tests that exemplify how hard it can be   ///
/// to use IAsyncEnumarable "by hand", and how mistakes can be made       ///
/// that can lead to occasional failings                                  ///
///                                                                       ///
/////////////////////////////////////////////////////////////////////////////


module ``PoC's for seq of tasks`` =

    [<Fact>]
    let ``Good: Show joining tasks with continuation is good`` () = task {
        // acts like a fold
        let! results = Gen.createAndJoinMultipleTasks 10 Gen.joinWithContinuation
        results |> should equal 10
    }

    [<Fact>]
    let ``Good: Show that joining tasks with 'bind' in task CE is good`` () = task {
        let! tasks = Gen.createAndJoinMultipleTasks 10 Gen.joinIdentityDelayed

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
        let! tasks = Gen.createAndJoinMultipleTasks 10 Gen.joinIdentityDelayed

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
        let! taskList = Gen.createAndJoinMultipleTasks 10 Gen.joinIdentityHotStarted

        // since tasks are hot-started, by this time they are already *all* running
        let! results =
            taskList
            |> List.map (Task.map Result<int, string>.Ok)
            |> List.traverseTaskResultA id

        match results with
        | Ok results ->
            // BAD!! As you can see, results are unequal to expected output
            results |> should not'
            <| equal (List.init (List.length results) ((+) 1))
        | Error err -> failwith $"Impossible: {err}"
    }

    [<Fact>]
    let ``Bad: Show that joining tasks as a list of tasks can be bad`` () = task {
        let! taskList = Gen.createAndJoinMultipleTasks 10 Gen.joinIdentityHotStarted

        // since tasks are hot-started, by this time they are already *all* running
        let tasks = taskList |> Array.ofList
        let results = Array.zeroCreate 10

        for i in 0..9 do
            let! item = tasks[i]
            results[i] <- item

        // BAD!! As you can see, results are unequal to expected output
        results |> should not'
        <| equal (Array.init (Array.length results) ((+) 1))
    }
