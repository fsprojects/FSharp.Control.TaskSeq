module TaskSeq.Tests.``taskSeqDynamic Computation Expression``

open System
open System.Threading
open System.Threading.Tasks

open Xunit
open FsUnit.Xunit

open FSharp.Control

// =====================================================================
// Tests for the dynamic (heap-allocated) path of the taskSeq CE.
// All tests use taskSeqDynamic { ... } to explicitly force the dynamic
// path (bypassing __stateMachine). This exercises the TaskSeqDynamic<'T>
// implementation introduced to fix issue #246.
// =====================================================================

// -------------------------
// Basic yield tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: empty sequence`` () = task {
    let ts = taskSeqDynamic { () }
    let! result = ts |> TaskSeq.toArrayAsync
    result |> should be Empty
}

[<Fact>]
let ``CE taskSeqDynamic: single yield`` () = task {
    let ts = taskSeqDynamic { yield 42 }
    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 42 |]
}

[<Fact>]
let ``CE taskSeqDynamic: multiple yields`` () = task {
    let ts = taskSeqDynamic {
        yield 1
        yield 2
        yield 3
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 2; 3 |]
}

[<Fact>]
let ``CE taskSeqDynamic: yield sequence 1 to 10`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..10 do
            yield i
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1..10 |]
}

[<Fact>]
let ``CE taskSeqDynamic: yield many items`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..1000 do
            yield i
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1..1000 |]
}

// -------------------------
// yield! tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: yield! from array-backed sequence`` () = task {
    let ts = taskSeqDynamic { yield! [ 1; 2; 3 ] }
    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 2; 3 |]
}

[<Fact>]
let ``CE taskSeqDynamic: yield! from taskSeq`` () = task {
    let inner = taskSeq { yield! [ 10; 20; 30 ] }

    let ts = taskSeqDynamic { yield! inner }
    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 10; 20; 30 |]
}

[<Fact>]
let ``CE taskSeqDynamic: multiple yield! interleaved with yield`` () = task {
    let ts = taskSeqDynamic {
        yield 0
        yield! [ 1; 2; 3 ]
        yield 4
        yield! [ 5; 6 ]
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 0; 1; 2; 3; 4; 5; 6 |]
}

// -------------------------
// Async binding tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: bind completed task`` () = task {
    let ts = taskSeqDynamic {
        let! x = Task.FromResult 42
        yield x
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 42 |]
}

[<Fact>]
let ``CE taskSeqDynamic: bind multiple tasks`` () = task {
    let ts = taskSeqDynamic {
        let! a = Task.FromResult 1
        let! b = Task.FromResult 2
        let! c = Task.FromResult 3
        yield a + b + c
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 6 |]
}

[<Fact>]
let ``CE taskSeqDynamic: bind ValueTask`` () = task {
    let ts = taskSeqDynamic {
        let! x = ValueTask.fromResult 99
        yield x
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 99 |]
}

[<Fact>]
let ``CE taskSeqDynamic: bind async computation`` () = task {
    let ts = taskSeqDynamic {
        let! x = async { return 77 }
        yield x
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 77 |]
}

[<Fact>]
let ``CE taskSeqDynamic: bind delayed task with yield`` () = task {
    let ts = taskSeqDynamic {
        let! x = task { return 1 }
        yield x
        let! y = task { return 2 }
        yield y
        let! z = task { return 3 }
        yield z
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 2; 3 |]
}

// -------------------------
// try/with tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: try/with, no exception`` () = task {
    let ts = taskSeqDynamic {
        try
            yield 1
            yield 2
        with _ ->
            yield -1
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 2 |]
}

[<Fact>]
let ``CE taskSeqDynamic: try/with, exception caught`` () = task {
    let ts = taskSeqDynamic {
        try
            yield 1
            raise (InvalidOperationException "test")
            yield 2
        with :? InvalidOperationException ->
            yield 99
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 99 |]
}

[<Fact>]
let ``CE taskSeqDynamic: try/with, exception propagated`` () = task {
    let ts = taskSeqDynamic {
        try
            raise (InvalidOperationException "test")
        with :? ArgumentException ->
            yield 99 // this handler doesn't match
    }

    fun () -> ts |> TaskSeq.toArrayAsync |> Task.ignore
    |> should throwAsync typeof<InvalidOperationException>
}

// -------------------------
// try/finally tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: try/finally, runs compensation on normal exit`` () = task {
    let mutable finallyRan = false

    let ts = taskSeqDynamic {
        try
            yield 42
        finally
            finallyRan <- true
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 42 |]
    finallyRan |> should be True
}

[<Fact>]
let ``CE taskSeqDynamic: try/finally, runs compensation on exception`` () = task {
    let mutable finallyRan = false

    let ts = taskSeqDynamic {
        try
            yield 1
            raise (InvalidOperationException "test")
        finally
            finallyRan <- true
    }

    fun () -> ts |> TaskSeq.toArrayAsync |> Task.ignore
    |> should throwAsync typeof<InvalidOperationException>

    finallyRan |> should be True
}

// -------------------------
// use/using tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: use with IDisposable`` () = task {
    let mutable disposed = false

    let makeDisposable () =
        { new IDisposable with
            member _.Dispose() = disposed <- true }

    let ts = taskSeqDynamic {
        use _ = makeDisposable ()
        yield 42
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 42 |]
    disposed |> should be True
}

[<Fact>]
let ``CE taskSeqDynamic: use with IAsyncDisposable`` () = task {
    let mutable disposed = false

    let makeDisposable () =
        { new IAsyncDisposable with
            member _.DisposeAsync() =
                disposed <- true
                ValueTask.CompletedTask }

    let ts = taskSeqDynamic {
        use _ = makeDisposable ()
        yield 42
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 42 |]
    disposed |> should be True
}

// -------------------------
// for/while loop tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: for loop over seq`` () = task {
    let ts = taskSeqDynamic {
        for i in [ 1; 2; 3; 4; 5 ] do
            yield i * 2
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 2; 4; 6; 8; 10 |]
}

[<Fact>]
let ``CE taskSeqDynamic: while loop`` () = task {
    let ts = taskSeqDynamic {
        let mutable i = 0

        while i < 5 do
            yield i
            i <- i + 1
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 0; 1; 2; 3; 4 |]
}

[<Fact>]
let ``CE taskSeqDynamic: nested for loops`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..3 do
            for j in 1..3 do
                yield i * 10 + j
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 11; 12; 13; 21; 22; 23; 31; 32; 33 |]
}

[<Fact>]
let ``CE taskSeqDynamic: for loop over async sequence`` () = task {
    let inner = taskSeq {
        yield 1
        yield 2
        yield 3
    }

    let ts = taskSeqDynamic {
        for x in inner do
            yield x * 10
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 10; 20; 30 |]
}

// -------------------------
// Cancellation token tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: cancellation token is honored on MoveNextAsync`` () = task {
    use cts = new CancellationTokenSource()
    cts.Cancel()

    let ts = taskSeqDynamic {
        for i in 1..100 do
            yield i
    }

    use enumerator = ts.GetAsyncEnumerator(cts.Token)

    fun () -> enumerator.MoveNextAsync().AsTask() |> Task.ignore
    |> should throwAsync typeof<OperationCanceledException>
}

[<Fact>]
let ``CE taskSeqDynamic: cancellation token mid-enumeration`` () = task {
    use cts = new CancellationTokenSource()

    let ts = taskSeqDynamic {
        for i in 1..100 do
            yield i
    }

    use enumerator = ts.GetAsyncEnumerator(cts.Token)

    // consume first item
    let! hasFirst = enumerator.MoveNextAsync()
    hasFirst |> should be True
    enumerator.Current |> should equal 1

    // cancel
    cts.Cancel()

    // next move should throw
    fun () -> enumerator.MoveNextAsync().AsTask() |> Task.ignore
    |> should throwAsync typeof<OperationCanceledException>
}

// -------------------------
// GetAsyncEnumerator / re-enumeration tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: GetAsyncEnumerator creates independent enumerators`` () = task {
    let ts = taskSeqDynamic {
        yield 1
        yield 2
        yield 3
    }

    let! first = ts |> TaskSeq.toArrayAsync
    let! second = ts |> TaskSeq.toArrayAsync

    first |> should equal [| 1; 2; 3 |]
    second |> should equal [| 1; 2; 3 |]
}

[<Fact>]
let ``CE taskSeqDynamic: enumerator DisposeAsync releases resources`` () = task {
    let mutable disposeCount = 0

    let ts = taskSeqDynamic {
        use _ =
            { new IDisposable with
                member _.Dispose() = disposeCount <- disposeCount + 1 }

        yield 1
        yield 2
    }

    let enumerator = ts.GetAsyncEnumerator(CancellationToken.None)

    let! hasFirst = enumerator.MoveNextAsync()
    hasFirst |> should be True

    do! enumerator.DisposeAsync()
    disposeCount |> should equal 1
}

// -------------------------
// Exception propagation tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: exception from yield sequence propagates`` () = task {
    let ts = taskSeqDynamic {
        yield 1
        raise (InvalidOperationException "error after yield")
        yield 2
    }

    fun () -> ts |> TaskSeq.toArrayAsync |> Task.ignore
    |> should throwAsync typeof<InvalidOperationException>
}

[<Fact>]
let ``CE taskSeqDynamic: exception from async bind propagates`` () = task {
    let failingTask = Task.FromException<int>(InvalidOperationException "task failed")

    let ts = taskSeqDynamic {
        let! x = failingTask
        yield x
    }

    fun () -> ts |> TaskSeq.toArrayAsync |> Task.ignore
    |> should throwAsync typeof<InvalidOperationException>
}

// -------------------------
// Mixed async and sync tests
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: mix of sync yields and async binds`` () = task {
    let ts = taskSeqDynamic {
        yield 1
        let! x = Task.FromResult 2
        yield x
        yield 3
        let! y = Task.FromResult 4
        yield y
        yield 5
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 2; 3; 4; 5 |]
}

[<Fact>]
let ``CE taskSeqDynamic: conditional logic with yields`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..10 do
            if i % 2 = 0 then
                yield i
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 2; 4; 6; 8; 10 |]
}

[<Fact>]
let ``CE taskSeqDynamic: deeply nested structure`` () = task {
    let ts = taskSeqDynamic {
        try
            for i in 1..3 do
                try
                    let! x = Task.FromResult i
                    yield x
                with _ ->
                    yield -1
        finally
            ()
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 2; 3 |]
}

// -------------------------
// TaskSeq module function tests with dynamic path
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: map function works on dynamic sequence`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..5 do
            yield i
    }

    let! result = ts |> TaskSeq.map (fun x -> x * x) |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 4; 9; 16; 25 |]
}

[<Fact>]
let ``CE taskSeqDynamic: filter function works on dynamic sequence`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..10 do
            yield i
    }

    let! result = ts |> TaskSeq.filter (fun x -> x % 3 = 0) |> TaskSeq.toArrayAsync
    result |> should equal [| 3; 6; 9 |]
}

[<Fact>]
let ``CE taskSeqDynamic: fold function works on dynamic sequence`` () = task {
    let ts = taskSeqDynamic {
        for i in 1..10 do
            yield i
    }

    let! result = ts |> TaskSeq.fold (fun acc x -> acc + x) 0
    result |> should equal 55
}

[<Fact>]
let ``CE taskSeqDynamic: toListAsync on dynamic sequence`` () = task {
    let ts = taskSeqDynamic {
        yield! [ 10; 20; 30; 40; 50 ]
    }

    let! result = ts |> TaskSeq.toListAsync
    result |> should equal [ 10; 20; 30; 40; 50 ]
}

// -------------------------
// Interop tests: static taskSeq and dynamic taskSeqDynamic
// -------------------------

[<Fact>]
let ``CE taskSeqDynamic: yield! from static taskSeq into dynamic`` () = task {
    let staticSeq = taskSeq {
        for i in 1..5 do
            yield i
    }

    let ts = taskSeqDynamic {
        yield! staticSeq
        yield 99
    }

    let! result = ts |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 2; 3; 4; 5; 99 |]
}

[<Fact>]
let ``CE taskSeqDynamic: can be iterated with standard taskSeq operators`` () = task {
    let dynamic = taskSeqDynamic {
        for i in 1..5 do
            yield i
    }

    let combined = taskSeq {
        yield! dynamic
        yield 99
    }

    let! result = combined |> TaskSeq.toArrayAsync
    result |> should equal [| 1; 2; 3; 4; 5; 99 |]
}

[<Fact>]
let ``CE taskSeqDynamic: TaskSeq module functions treat dynamic and static sequences identically`` () = task {
    let staticResult =
        taskSeq {
            for i in 1..100 do
                yield i
        }
        |> TaskSeq.filter (fun x -> x % 7 = 0)
        |> TaskSeq.map (fun x -> x * 2)
        |> TaskSeq.toArrayAsync

    let dynamicResult =
        taskSeqDynamic {
            for i in 1..100 do
                yield i
        }
        |> TaskSeq.filter (fun x -> x % 7 = 0)
        |> TaskSeq.map (fun x -> x * 2)
        |> TaskSeq.toArrayAsync

    let! s = staticResult
    let! d = dynamicResult
    s |> should equal d
}
