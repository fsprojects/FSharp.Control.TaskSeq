module TaskSeq.Tests.Replicate

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.replicate
//

module EmptySeq =
    [<Fact>]
    let ``TaskSeq-replicate with count 0 gives empty sequence`` () = TaskSeq.replicate 0 42 |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-replicate with negative count gives an error`` () =
        fun () ->
            TaskSeq.replicate -1 42
            |> TaskSeq.toArrayAsync
            |> Task.ignore

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            TaskSeq.replicate Int32.MinValue "hello"
            |> TaskSeq.toArrayAsync
            |> Task.ignore

        |> should throwAsyncExact typeof<ArgumentException>

module Immutable =
    [<Fact>]
    let ``TaskSeq-replicate produces the correct count and value`` () = task {
        let! arr = TaskSeq.replicate 5 99 |> TaskSeq.toArrayAsync
        arr |> should haveLength 5
        arr |> should equal [| 99; 99; 99; 99; 99 |]
    }

    [<Fact>]
    let ``TaskSeq-replicate with count 1 produces a singleton`` () = task {
        let! arr = TaskSeq.replicate 1 "x" |> TaskSeq.toArrayAsync
        arr |> should haveLength 1
        arr[0] |> should equal "x"
    }

    [<Fact>]
    let ``TaskSeq-replicate with large count`` () = task {
        let count = 10_000
        let! arr = TaskSeq.replicate count 7 |> TaskSeq.toArrayAsync
        arr |> should haveLength count
        arr |> Array.forall ((=) 7) |> should be True
    }

    [<Fact>]
    let ``TaskSeq-replicate works with null as value`` () = task {
        let! arr = TaskSeq.replicate 3 null |> TaskSeq.toArrayAsync
        arr |> should haveLength 3
        arr |> Array.forall (fun x -> x = null) |> should be True
    }

    [<Fact>]
    let ``TaskSeq-replicate can be consumed multiple times`` () = task {
        let ts = TaskSeq.replicate 4 "a"
        let! arr1 = ts |> TaskSeq.toArrayAsync
        let! arr2 = ts |> TaskSeq.toArrayAsync
        arr1 |> should equal arr2
    }

module SideEffects =
    [<Fact>]
    let ``TaskSeq-replicate with a mutable value captures the value, not a reference`` () = task {
        let mutable x = 1
        let ts = TaskSeq.replicate 3 x
        x <- 999
        let! arr = ts |> TaskSeq.toArrayAsync
        // replicate captures the value at call time (value type)
        arr |> should equal [| 1; 1; 1 |]
    }
