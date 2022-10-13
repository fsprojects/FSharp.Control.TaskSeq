module FSharpy.TaskSeq.Tests.``taskSeq Computation Expression``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Fact>]
let ``CE taskSeq with several yield!`` () = task {
    let tskSeq = taskSeq {
        yield! createDummyTaskSeq 10
        yield! createDummyTaskSeq 5
        yield! createDummyTaskSeq 10
        yield! createDummyTaskSeq 5
    }

    let! data = tskSeq |> TaskSeq.toListAsync

    data
    |> should equal (List.concat [ [ 1..10 ]; [ 1..5 ]; [ 1..10 ]; [ 1..5 ] ])
}

[<Fact>]
let ``CE taskSeq with several return!`` () = task {
    // TODO: should we even support this? Traditional 'seq' doesn't.
    let tskSeq = taskSeq {
        return! createDummyTaskSeq 10
        return! createDummyTaskSeq 5
    }

    let! data = tskSeq |> TaskSeq.toListAsync

    // FIXME!!! This behavior is *probably* not correct
    data |> should equal [ 1..10 ]
}


[<Fact>]
let ``CE taskSeq with mixing yield! and yield`` () = task {
    let tskSeq = taskSeq {
        yield! createDummyTaskSeq 10
        yield 42
        yield! createDummyTaskSeq 5
        yield 42
        yield! createDummyTaskSeq 10
        yield 42
        yield! createDummyTaskSeq 5
    }

    let! data = tskSeq |> TaskSeq.toListAsync

    data
    |> should equal (List.concat [ [ 1..10 ]; [ 42 ]; [ 1..5 ]; [ 42 ]; [ 1..10 ]; [ 42 ]; [ 1..5 ] ])
}

[<Fact>]
let ``CE taskSeq with nested deeply yield! perf test`` () = task {
    let control = seq {
        yield! [ 1..10 ]

        // original:
        // yield! Seq.concat <| Seq.init 4251 (fun _ -> [ 1; 2 ])
        yield! Seq.concat <| Seq.init 120 (fun _ -> [ 1; 2 ])
    }

    //let createTasks = createDummyTaskSeqWith 1L<µs> 10L<µs>
    // FIXME: it appears that deeply nesting adds to performance degradation, need to benchmark/profile this
    // probably cause: since this is *fast* with DirectTask, the reason is likely the way the Task.Delay causes
    // *many* subtasks to be delayed, resulting in exponential delay. Reason: max accuracy of Delay is about 15ms (!)
    let tskSeq = taskSeq {
        yield! createDummyTaskSeq 10

        // nestings amount to 8512 sequences of [1;2]
        for i in 0..2 do
            yield! createDummyTaskSeq 2

            for i in 0..2 do
                yield! createDummyTaskSeq 2

                for i in 0..2 do
                    yield! createDummyTaskSeq 2

                    for i in 0..2 do
                        yield! createDummyTaskSeq 2
                        // stopping here, at a total 250 nested taskSeq
                        // add the below to get to 4300

                        //for i in 0..2 do
                        //    yield! createDummyTaskSeq 2

                        //    for i in 0..2 do
                        //        yield! createDummyTaskSeq 2

                        //for i in 0..2 do
                        //    yield! createDummyTaskSeq 2

                        //    for i in 0..2 do
                        //        yield! createDummyTaskSeq 2

                        //        for i in 0..2 do
                        //            yield! createDummyTaskSeq 2
                        yield! TaskSeq.empty
    }

    let! data = tskSeq |> TaskSeq.toListAsync
    data |> List.length |> should equal 250 // 8512
    data |> should equal (List.ofSeq control)
}

[<Fact>]
let ``CE taskSeq: 500 TaskDelay-delayed tasks using yield!`` () = task {
    // runs in 10-15s because of Task.Delay between 10-30ms
    // should generally be about as fast as `task`, see below
    let tskSeq = taskSeq { yield! createDummyTaskSeq 500 }

    let! data = tskSeq |> TaskSeq.toListAsync

    data |> should equal [ 1..500 ]
}

[<Fact>]
let ``CE taskSeq: 500 sync-running tasks using yield!`` () = task {
    // runs in a few 10's of ms, because of absense of Task.Delay
    // should generally be about as fast as `task`, see below
    let tskSeq = taskSeq { yield! createDummyDirectTaskSeq 500 }

    let! data = tskSeq |> TaskSeq.toListAsync

    data |> should equal [ 1..500 ]
}

[<Fact>]
let ``CE taskSeq: 5000 sync-running tasks using yield!`` () = task {
    // runs in a few 100's of ms, because of absense of Task.Delay
    // should generally be about as fast as `task`, see below
    let tskSeq = taskSeq { yield! createDummyDirectTaskSeq 5000 }

    let! data = tskSeq |> TaskSeq.toListAsync

    data |> should equal [ 1..5000 ]
}

[<Fact>]
let ``CE task: 500 TaskDelay-delayed tasks using for-loop`` () = task {
    // runs in 10-15s because of Task.Delay between 10-30ms
    // for smoke-test comparison with taskSeq
    let tasks = DummyTaskFactory().CreateDelayedTasks 500
    let mutable i = 0

    for t in tasks do
        i <- i + 1
        do! t () |> Task.ignore

    i |> should equal 500
}

[<Fact>]
let ``CE task: 500 list of sync-running tasks using for-loop`` () = task {
    // runs in a few 10's of ms, because of absense of Task.Delay
    // for smoke-test comparison with taskSeq
    let tasks = DummyTaskFactory().CreateDirectTasks 500
    let mutable i = 0

    for t in tasks do
        i <- i + 1
        do! t () |> Task.ignore

    i |> should equal 500
}

[<Fact>]
let ``CE task: 5000 list of sync-running tasks using for-loop`` () = task {
    // runs in a few 100's of ms, because of absense of Task.Delay
    // for smoke-test comparison with taskSeq
    let tasks = DummyTaskFactory().CreateDirectTasks 5000
    let mutable i = 0

    for t in tasks do
        i <- i + 1
        do! t () |> Task.ignore

    i |> should equal 5000
}
