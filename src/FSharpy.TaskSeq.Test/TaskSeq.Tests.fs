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
