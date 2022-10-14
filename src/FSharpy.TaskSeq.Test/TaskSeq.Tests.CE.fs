module FSharpy.Tests.``taskSeq Computation Expression``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy
open System.Threading.Tasks
open System.Diagnostics


[<Fact(Timeout = 10_000)>]
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

[<Fact(Timeout = 10_000)>]
let ``CE taskSeq with nested yield!`` () = task {
    let control = seq {
        yield! [ 1..10 ]

        for i in 0..9 do
            yield! [ 1..2 ]

            for i in 0..2 do
                yield! seq { yield 42 }

                for i in 100..102 do
                    yield! seq { yield! seq { yield i } }
    }

    let tskSeq = taskSeq {
        yield! createDummyTaskSeq 10

        for i in 0..9 do
            yield! createDummyTaskSeq 2

            for i in 0..2 do
                yield! taskSeq { yield 42 }

                for i in 100..102 do
                    yield! taskSeq { yield! taskSeq { yield i } }
    }

    let! data = tskSeq |> TaskSeq.toListAsync

    data |> should equal (List.ofSeq control)
    data |> should haveLength 150
}

[<Fact(Timeout = 10_000)>]
let ``CE taskSeq with nested deeply yield! perf test 8521 nested tasks`` () = task {
    let control = seq {
        yield! [ 1..10 ]

        // original:
        yield! Seq.concat <| Seq.init 4251 (fun _ -> [ 1; 2 ])
    //yield! Seq.concat <| Seq.init 120 (fun _ -> [ 1; 2 ])
    }

    let createTasks = createDummyTaskSeqWith 1L<µs> 10L<µs>
    // FIXME: it appears that deeply nesting adds to performance degradation, need to benchmark/profile this
    // probably cause: since this is *fast* with DirectTask, the reason is likely the way the Task.Delay causes
    // *many* subtasks to be delayed, resulting in exponential delay. Reason: max accuracy of Delay is about 15ms (!)

    // RESOLUTION: seems to have been caused by erratic Task.Delay which has only a 15ms resolution
    let tskSeq = taskSeq {
        yield! createTasks 10

        // nestings amount to 8512 sequences of [1;2]
        for i in 0..2 do
            yield! createTasks 2

            for i in 0..2 do
                yield! createTasks 2

                for i in 0..2 do
                    yield! createTasks 2

                    for i in 0..2 do
                        yield! createTasks 2

                        for i in 0..2 do
                            yield! createTasks 2

                            for i in 0..2 do
                                yield! createTasks 2

                        for i in 0..2 do
                            yield! createTasks 2

                            for i in 0..2 do
                                yield! createTasks 2

                                for i in 0..2 do
                                    yield! createTasks 2

                        yield! TaskSeq.empty
    }

    let! data = tskSeq |> TaskSeq.toListAsync
    data |> List.length |> should equal 8512
    data |> should equal (List.ofSeq control)
}

[<Fact(Timeout = 10_000)>]
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


[<Fact(Timeout = 10_000)>]
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

[<Fact(Timeout = 10_000)>]
let ``CE taskSeq: 1000 TaskDelay-delayed tasks using yield!`` () = task {
    // Smoke performance test
    // Runs in slightly over half a second (average of spin-wait, plus small overhead)
    // should generally be about as fast as `task`, see below for equivalent test.
    let tskSeq = taskSeq { yield! createDummyTaskSeqWith 50L<µs> 1000L<µs> 1000 }
    let! data = tskSeq |> TaskSeq.toListAsync
    data |> should equal [ 1..1000 ]
}

[<Fact(Timeout = 10_000)>]
let ``CE taskSeq: 1000 sync-running tasks using yield!`` () = task {
    // Smoke performance test
    // Runs in a few 10's of ms, because of absense of Task.Delay
    // should generally be about as fast as `task`, see below
    let tskSeq = taskSeq { yield! createDummyDirectTaskSeq 1000 }
    let! data = tskSeq |> TaskSeq.toListAsync
    data |> should equal [ 1..1000 ]
}

[<Fact(Timeout = 10_000)>]
let ``CE taskSeq: 5000 sync-running tasks using yield!`` () = task {
    // Smoke performance test
    // Compare with task-ce test below. Uses a no-delay hot-started sequence of tasks.
    let tskSeq = taskSeq { yield! createDummyDirectTaskSeq 5000 }
    let! data = tskSeq |> TaskSeq.toListAsync
    data |> should equal [ 1..5000 ]
}

[<Fact(Timeout = 10_000)>]
let ``CE task: 1000 TaskDelay-delayed tasks using for-loop`` () = task {
    // Uses SpinWait for effective task-delaying
    // for smoke-test comparison with taskSeq
    let tasks = DummyTaskFactory(50L<µs>, 1000L<µs>).CreateDelayedTasks 1000
    let mutable i = 0

    for t in tasks do
        i <- i + 1
        do! t () |> Task.ignore

    i |> should equal 1000
}

[<Fact(Timeout = 10_000)>]
let ``CE task: 1000 list of sync-running tasks using for-loop`` () = task {
    // runs in a few 10's of ms, because of absense of Task.Delay
    // for smoke-test comparison with taskSeq
    let tasks = DummyTaskFactory().CreateDirectTasks 1000
    let mutable i = 0

    for t in tasks do
        i <- i + 1
        do! t () |> Task.ignore

    i |> should equal 1000
}

[<Fact(Timeout = 10_000)>]
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
