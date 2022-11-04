module TaskSeq.Tests.``taskSeq Computation Expression``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

[<Fact>]
let ``CE taskSeq with several yield!`` () = task {
    let tskSeq = taskSeq {
        yield! Gen.sideEffectTaskSeq 10
        yield! Gen.sideEffectTaskSeq 5
        yield! Gen.sideEffectTaskSeq 10
        yield! Gen.sideEffectTaskSeq 5
    }

    let! data = tskSeq |> TaskSeq.toListAsync

    data
    |> should equal (List.concat [ [ 1..10 ]; [ 1..5 ]; [ 1..10 ]; [ 1..5 ] ])
}

[<Fact>]
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
        yield! Gen.sideEffectTaskSeq 10

        for i in 0..9 do
            yield! Gen.sideEffectTaskSeq 2

            for i in 0..2 do
                yield! taskSeq { yield 42 }

                for i in 100..102 do
                    yield! taskSeq { yield! taskSeq { yield i } }
    }

    let! data = tskSeq |> TaskSeq.toListAsync

    data |> should equal (List.ofSeq control)
    data |> should haveLength 150
}

[<Fact>]
let ``CE taskSeq with nested deeply yield! perf test 8521 nested tasks`` () = task {
    let expected = seq {
        yield! [ 1..10 ]
        yield! Seq.concat <| Seq.init 4251 (fun _ -> [ 1; 2 ])
    }

    let createTasks = Gen.sideEffectTaskSeqMicro 1L<µs> 10L<µs>
    //
    // NOTES: it appears that deeply nesting adds to performance degradation, need to benchmark/profile this
    // probable cause: since this is *fast* with DirectTask, the reason is likely the way the Task.Delay causes
    // *many* subtasks to be delayed, resulting in exponential delay. Reason: max accuracy of Delay is about 15ms (!)
    //
    // RESOLUTION: seems to have been caused by erratic Task.Delay which has only a 15ms resolution
    //

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
    data |> should equal (List.ofSeq expected) // cannot compare seq this way, so, List.ofSeq it is
}

[<Fact>]
let ``CE taskSeq with mixing yield! and yield`` () = task {
    let tskSeq = taskSeq {
        yield! Gen.sideEffectTaskSeq 10
        yield 42
        yield! Gen.sideEffectTaskSeq 5
        yield 42
        yield! Gen.sideEffectTaskSeq 10
        yield 42
        yield! Gen.sideEffectTaskSeq 5
    }

    let! data = tskSeq |> TaskSeq.toListAsync

    data
    |> should equal (List.concat [ [ 1..10 ]; [ 42 ]; [ 1..5 ]; [ 42 ]; [ 1..10 ]; [ 42 ]; [ 1..5 ] ])
}

[<Fact>]
let ``CE taskSeq: 1000 TaskDelay-delayed tasks using yield!`` () = task {
    // Smoke performance test
    // Runs in slightly over half a second (average of spin-wait, plus small overhead)
    // should generally be about as fast as `task`, see below for equivalent test.
    let tskSeq = taskSeq { yield! Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 1000 }
    let! data = tskSeq |> TaskSeq.toListAsync
    data |> should equal [ 1..1000 ]
}

[<Fact>]
let ``CE taskSeq: 1000 sync-running tasks using yield!`` () = task {
    // Smoke performance test
    // Runs in a few 10's of ms, because of absense of Task.Delay
    // should generally be about as fast as `task`, see below
    let tskSeq = taskSeq { yield! Gen.sideEffectTaskSeq_Sequential 1000 }
    let! data = tskSeq |> TaskSeq.toListAsync
    data |> should equal [ 1..1000 ]
}

[<Fact>]
let ``CE taskSeq: 5000 sync-running tasks using yield!`` () = task {
    // Smoke performance test
    // Compare with task-ce test below. Uses a no-delay hot-started sequence of tasks.
    let tskSeq = taskSeq { yield! Gen.sideEffectTaskSeq_Sequential 5000 }
    let! data = tskSeq |> TaskSeq.toListAsync
    data |> should equal [ 1..5000 ]
}

[<Fact>]
let ``CE task: 1000 TaskDelay-delayed tasks using for-loop`` () = task {
    // Uses SpinWait for effective task-delaying
    // for smoke-test comparison with taskSeq
    let tasks = DummyTaskFactory(50L<µs>, 1000L<µs>).CreateDelayedTasks_SideEffect 1000
    let mutable i = 0

    for t in tasks do
        i <- i + 1
        do! t () |> Task.ignore

    i |> should equal 1000
}

[<Fact>]
let ``CE task: 1000 list of sync-running tasks using for-loop`` () = task {
    // runs in a few 10's of ms, because of absense of Task.Delay
    // for smoke-test comparison with taskSeq
    let tasks = DummyTaskFactory().CreateDirectTasks_SideEffect 1000
    let mutable i = 0

    for t in tasks do
        i <- i + 1
        do! t () |> Task.ignore

    i |> should equal 1000
}

[<Fact>]
let ``CE task: 5000 list of sync-running tasks using for-loop`` () = task {
    // runs in a few 100's of ms, because of absense of Task.Delay
    // for smoke-test comparison with taskSeq
    let tasks = DummyTaskFactory().CreateDirectTasks_SideEffect 5000
    let mutable i = 0

    for t in tasks do
        i <- i + 1
        do! t () |> Task.ignore

    i |> should equal 5000
}
