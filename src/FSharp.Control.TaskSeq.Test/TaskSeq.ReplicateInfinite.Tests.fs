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
