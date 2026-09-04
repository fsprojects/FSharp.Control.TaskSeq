module TaskSeq.Tests.ReplicateInfinite

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.replicateInfinite
// TaskSeq.replicateInfiniteAsync
// TaskSeq.replicateUntilNoneAsync
//

module ReplicateInfinite =
    [<Fact>]
    let ``TaskSeq-replicateInfinite yields value indefinitely`` () = task {
        let! arr =
            TaskSeq.replicateInfinite 7
            |> TaskSeq.take 5
            |> TaskSeq.toArrayAsync

        arr |> should equal [| 7; 7; 7; 7; 7 |]
    }

    [<Fact>]
    let ``TaskSeq-replicateInfinite with take 0 gives empty`` () = TaskSeq.replicateInfinite 1 |> TaskSeq.take 0 |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-replicateInfinite can be consumed multiple times`` () = task {
        let ts = TaskSeq.replicateInfinite "x"
        let! arr1 = ts |> TaskSeq.take 3 |> TaskSeq.toArrayAsync
        let! arr2 = ts |> TaskSeq.take 3 |> TaskSeq.toArrayAsync
        arr1 |> should equal [| "x"; "x"; "x" |]
        arr2 |> should equal arr1
    }

    [<Fact>]
    let ``TaskSeq-replicateInfinite with large take`` () = task {
        let count = 10_000

        let! arr =
            TaskSeq.replicateInfinite 42
            |> TaskSeq.take count
            |> TaskSeq.toArrayAsync

        arr |> should haveLength count
        arr |> Array.forall ((=) 42) |> should be True
    }

    [<Fact>]
    let ``TaskSeq-replicateInfinite value captured at call site`` () = task {
        let mutable x = 1
        let ts = TaskSeq.replicateInfinite x
        x <- 999
        let! arr = ts |> TaskSeq.take 3 |> TaskSeq.toArrayAsync
        // value type is captured at call time
        arr |> should equal [| 1; 1; 1 |]
    }


module ReplicateInfiniteAsync =
    [<Fact>]
    let ``TaskSeq-replicateInfiniteAsync yields computed value indefinitely`` () = task {
        let mutable n = 0

        let comp () = task {
            n <- n + 1
            return n
        }

        let! arr =
            TaskSeq.replicateInfiniteAsync comp
            |> TaskSeq.take 4
            |> TaskSeq.toArrayAsync

        arr |> should equal [| 1; 2; 3; 4 |]
    }

    [<Fact>]
    let ``TaskSeq-replicateInfiniteAsync with take 0 gives empty`` () =
        let comp () = Task.fromResult 99

        TaskSeq.replicateInfiniteAsync comp
        |> TaskSeq.take 0
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-replicateInfiniteAsync constant computation`` () = task {
        let comp () = Task.fromResult "hello"

        let! arr =
            TaskSeq.replicateInfiniteAsync comp
            |> TaskSeq.take 3
            |> TaskSeq.toArrayAsync

        arr |> should equal [| "hello"; "hello"; "hello" |]
    }


module ReplicateUntilNoneAsync =
    [<Fact>]
    let ``TaskSeq-replicateUntilNoneAsync stops on None`` () = task {
        let mutable n = 0

        let comp () = task {
            n <- n + 1

            if n <= 3 then return Some n else return None
        }

        let! arr = TaskSeq.replicateUntilNoneAsync comp |> TaskSeq.toArrayAsync
        arr |> should equal [| 1; 2; 3 |]
    }

    [<Fact>]
    let ``TaskSeq-replicateUntilNoneAsync returns empty when first call is None`` () = task {
        let comp () = Task.fromResult None
        let ts = TaskSeq.replicateUntilNoneAsync comp
        let! arr = ts |> TaskSeq.toArrayAsync
        arr |> should haveLength 0
    }

    [<Fact>]
    let ``TaskSeq-replicateUntilNoneAsync yields single element`` () = task {
        let mutable called = false

        let comp () = task {
            if not called then
                called <- true
                return Some 42
            else
                return None
        }

        let! arr = TaskSeq.replicateUntilNoneAsync comp |> TaskSeq.toArrayAsync
        arr |> should equal [| 42 |]
    }

    [<Fact>]
    let ``TaskSeq-replicateUntilNoneAsync with counter`` () = task {
        let count = 100
        let mutable i = 0

        let comp () = task {
            if i < count then
                i <- i + 1
                return Some i
            else
                return None
        }

        let! arr = TaskSeq.replicateUntilNoneAsync comp |> TaskSeq.toArrayAsync
        arr |> should haveLength count
        arr[0] |> should equal 1
        arr[count - 1] |> should equal count
    }


module SideEffects =
    [<Fact>]
    let ``TaskSeq-replicateInfiniteAsync re-runs the computation on each fresh enumeration`` () = task {
        let mutable calls = 0

        let comp () = task {
            calls <- calls + 1
            return calls
        }

        let ts = TaskSeq.replicateInfiniteAsync comp

        let! arr1 = ts |> TaskSeq.take 3 |> TaskSeq.toArrayAsync
        arr1 |> should equal [| 1; 2; 3 |]
        calls |> should equal 3

        // a fresh enumeration starts the computation from scratch; side effects
        // (here, the call counter) keep accumulating across enumerations
        let! arr2 = ts |> TaskSeq.take 2 |> TaskSeq.toArrayAsync
        arr2 |> should equal [| 4; 5 |]
        calls |> should equal 5
    }

    [<Fact>]
    let ``TaskSeq-replicateUntilNoneAsync re-runs the computation from its initial state on each fresh enumeration`` () = task {
        let mutable totalCalls = 0

        let comp () = task {
            let mutable n = 0
            totalCalls <- totalCalls + 1

            if n <= 1 then
                n <- n + 1
                return Some n
            else
                return None
        }

        let ts = TaskSeq.replicateUntilNoneAsync comp

        let! arr1 = ts |> TaskSeq.toArrayAsync
        arr1 |> should equal [| 1 |]
        totalCalls |> should equal 2

        // re-enumerating re-invokes the generator function itself (state is local
        // to each call), so side effects on shared state accumulate further
        let! arr2 = ts |> TaskSeq.toArrayAsync
        arr2 |> should equal [| 1 |]
        totalCalls |> should equal 4
    }

    [<Fact>]
    let ``TaskSeq-replicateInfinite abandoning enumeration early does not affect a later fresh enumeration`` () = task {
        let ts = TaskSeq.replicateInfinite 3

        // partially enumerate and abandon (dispose) without reaching a natural end
        use enum1 = ts.GetAsyncEnumerator System.Threading.CancellationToken.None
        let! hasNext = enum1.MoveNextAsync()
        hasNext |> should be True
        enum1.Current |> should equal 3
        do! enum1.DisposeAsync()

        // a fresh enumerator over the same taskSeq starts cleanly from the beginning
        let! arr = ts |> TaskSeq.take 3 |> TaskSeq.toArrayAsync
        arr |> should equal [| 3; 3; 3 |]
    }
