namespace FSharpy.Tests

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

/////////////////////////////////////////////////////////////////////////////
///                                                                       ///
/// This file contains bunch of tests that exemplify how hard it can be   ///
/// to use IAsyncEnumarable "by hand", and how mistakes can be made       ///
/// that can lead to occasional failings                                  ///
///                                                                       ///
/////////////////////////////////////////////////////////////////////////////


type ``PoC's for seq of tasks``(output) =

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``Good: Show joining tasks with continuation is good`` () =
        logStart output

        task {
            // acts like a fold
            let! results = createAndJoinMultipleTasks 10 joinWithContinuation
            results |> should equal 10
        }

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``Good: Show that joining tasks with 'bind' in task CE is good`` () =
        logStart output

        task {
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

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``Good: Show that joining tasks with 'taskSeq' is good`` () =
        logStart output

        task {
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

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``Bad: Show that joining tasks with 'traverseTaskResult' can be bad`` () =
        logStart output

        task {
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

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``Bad: Show that joining tasks as a list of tasks can be bad`` () =
        logStart output

        task {
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
