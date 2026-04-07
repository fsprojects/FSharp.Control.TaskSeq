module TaskSeq.Tests.``taskSeqDynamic Computation Expression``

open System
open System.Threading

open Xunit
open FsUnit.Xunit

open FSharp.Control

// -------------------------------------------------------
// Basic sanity tests for the taskSeqDynamic CE builder.
// taskSeqDynamic uses the same static path as taskSeq when
// compiled normally, but falls back to the dynamic
// (ResumptionDynamicInfo-based) path in FSI / when the
// F# compiler cannot emit static resumable code.
// -------------------------------------------------------

[<Fact>]
let ``CE taskSeqDynamic empty sequence`` () = task {
    let ts = taskSeqDynamic { () }
    let! data = ts |> TaskSeq.toListAsync
    data |> should be Empty
}

[<Fact>]
let ``CE taskSeqDynamic single yield`` () = task {
    let ts = taskSeqDynamic { yield 42 }
    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 42 ]
}

[<Fact>]
let ``CE taskSeqDynamic multiple yields`` () = task {
    let ts = taskSeqDynamic {
        yield 1
        yield 2
        yield 3
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3 ]
}

[<Fact>]
let ``CE taskSeqDynamic yield with for loop`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..5 do
            yield i
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3; 4; 5 ]
}

[<Fact>]
let ``CE taskSeqDynamic yield with async task bind`` () = task {
    let ts = taskSeqDynamic {
        let! x = task { return 10 }
        yield x
        let! y = task { return 20 }
        yield y
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 10; 20 ]
}

[<Fact>]
let ``CE taskSeqDynamic yield from seq`` () = task {
    let ts = taskSeqDynamic { yield! [ 1; 2; 3 ] }
    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3 ]
}

[<Fact>]
let ``CE taskSeqDynamic yield from another taskSeqDynamic`` () = task {
    let inner = taskSeqDynamic {
        yield 1
        yield 2
    }

    let ts = taskSeqDynamic {
        yield! inner
        yield 3
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1; 2; 3 ]
}

[<Fact>]
let ``CE taskSeqDynamic yield from taskSeq`` () = task {
    let inner = taskSeq {
        yield 1
        yield 2
    }

    let ts = taskSeqDynamic { yield! inner }
    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1; 2 ]
}

[<Fact>]
let ``CE taskSeqDynamic with tryWith`` () = task {
    let ts = taskSeqDynamic {
        try
            yield 1
            yield 2
        with _ ->
            yield -1
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1; 2 ]
}

[<Fact>]
let ``CE taskSeqDynamic with tryWith catching exception`` () = task {
    let ts = taskSeqDynamic {
        try
            yield 1
            raise (InvalidOperationException "test")
            yield 2
        with :? InvalidOperationException ->
            yield 99
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1; 99 ]
}

[<Fact>]
let ``CE taskSeqDynamic with tryFinally`` () = task {
    let mutable finallyCalled = false

    let ts = taskSeqDynamic {
        try
            yield 1
            yield 2
        finally
            finallyCalled <- true
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1; 2 ]
    finallyCalled |> should equal true
}

[<Fact>]
let ``CE taskSeqDynamic with use`` () = task {
    let mutable disposed = false

    let mkDisposable () =
        { new IDisposable with
            member _.Dispose() = disposed <- true
        }

    let ts = taskSeqDynamic {
        use _d = mkDisposable ()
        yield 42
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 42 ]
    disposed |> should equal true
}

[<Fact>]
let ``CE taskSeqDynamic supports re-enumeration`` () = task {
    let ts = taskSeqDynamic {
        yield 1
        yield 2
        yield 3
    }

    let! data1 = ts |> TaskSeq.toListAsync
    let! data2 = ts |> TaskSeq.toListAsync
    data1 |> should equal [ 1; 2; 3 ]
    data2 |> should equal [ 1; 2; 3 ]
}

[<Fact>]
let ``CE taskSeqDynamic multiple re-enumerations produce same result`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..10 do
            yield i
    }

    for _ in 1..5 do
        let! data = ts |> TaskSeq.toListAsync
        data |> should equal [ 1..10 ]
}

[<Fact>]
let ``CE taskSeqDynamic with cancellation`` () = task {
    use cts = new CancellationTokenSource()
    cts.Cancel()

    let ts = taskSeqDynamic {
        yield 1
        yield 2
        yield 3
    }

    let enumerator = ts.GetAsyncEnumerator(cts.Token)

    let mutable threw = false

    try
        let! _ = enumerator.MoveNextAsync()
        ()
    with :? OperationCanceledException ->
        threw <- true

    threw |> should equal true
    do! enumerator.DisposeAsync()
}

[<Fact>]
let ``CE taskSeqDynamic with large for loop`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..1000 do
            yield i
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1..1000 ]
    data |> should haveLength 1000
}

[<Fact>]
let ``CE taskSeqDynamic with nested for loops`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..3 do
            for j in 1..3 do
                yield i * 10 + j
    }

    let expected = [
        for i in 1..3 do
            for j in 1..3 do
                yield i * 10 + j
    ]

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal expected
}

[<Fact>]
let ``CE taskSeqDynamic with async value task bind`` () = task {
    let ts = taskSeqDynamic {
        let! x = System.Threading.Tasks.ValueTask.FromResult(7)
        yield x
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 7 ]
}

[<Fact>]
let ``CE taskSeqDynamic with Async bind`` () = task {
    let ts = taskSeqDynamic {
        let! x = async { return 99 }
        yield x
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 99 ]
}

[<Fact>]
let ``CE taskSeqDynamic is IAsyncEnumerable`` () = task {
    // In compiled mode this uses the static path (returns TaskSeq<_,_>),
    // in FSI it returns TaskSeqDynamic<_>. Both implement IAsyncEnumerable<int>.
    let ts = taskSeqDynamic { yield 1 }
    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 1 ]
}

[<Fact>]
let ``CE taskSeqDynamic empty produces no values`` () = task {
    let mutable count = 0

    let ts = taskSeqDynamic { () }

    let e = ts.GetAsyncEnumerator(CancellationToken.None)

    try
        while! e.MoveNextAsync() do
            count <- count + 1
    finally
        ()

    do! e.DisposeAsync()
    count |> should equal 0
}

[<Fact>]
let ``CE taskSeqDynamic with while loop`` () = task {
    let ts = taskSeqDynamic {
        let mutable i = 0

        while i < 5 do
            yield i
            i <- i + 1
    }

    let! data = ts |> TaskSeq.toListAsync
    data |> should equal [ 0; 1; 2; 3; 4 ]
}

[<Fact>]
let ``CE taskSeqDynamic is same type as TaskSeq`` () =
    let ts: TaskSeq<int> = taskSeqDynamic { yield 1 }
    ts |> should not' (be Null)

[<Fact>]
let ``CE taskSeqDynamic with several yield!`` () = task {
    let tskSeq = taskSeqDynamic {
        yield! Gen.sideEffectTaskSeq 10
        yield! Gen.sideEffectTaskSeq 5
    }

    let! data = tskSeq |> TaskSeq.toListAsync
    data |> should equal (List.concat [ [ 1..10 ]; [ 1..5 ] ])
}
