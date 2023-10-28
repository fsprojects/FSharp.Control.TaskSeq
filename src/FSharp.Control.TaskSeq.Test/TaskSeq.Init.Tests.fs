module TaskSeq.Tests.Init

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.init
// TaskSeq.initInfinite
// TaskSeq.initAsync
// TaskSeq.initInfiniteAsync
//

/// Asserts that a sequence contains the char values 'A'..'J'.

module EmptySeq =
    [<Fact>]
    let ``TaskSeq-init can generate an empty sequence`` () = TaskSeq.init 0 (fun x -> x) |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-initAsync can generate an empty sequence`` () =
        TaskSeq.initAsync 0 (fun x -> Task.fromResult x)
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-init with a negative count gives an error`` () =
        fun () ->
            TaskSeq.init -1 (fun x -> Task.fromResult x)
            |> TaskSeq.toArrayAsync
            |> Task.ignore

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            TaskSeq.init Int32.MinValue (fun x -> Task.fromResult x)
            |> TaskSeq.toArrayAsync
            |> Task.ignore

        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-initAsync with a negative count gives an error`` () =
        fun () ->
            TaskSeq.initAsync Int32.MinValue (fun x -> Task.fromResult x)
            |> TaskSeq.toArrayAsync
            |> Task.ignore

        |> should throwAsyncExact typeof<ArgumentException>

module Immutable =
    [<Fact>]
    let ``TaskSeq-init singleton`` () =
        TaskSeq.init 1 id
        |> TaskSeq.head
        |> Task.map (should equal 0)

    [<Fact>]
    let ``TaskSeq-initAsync singleton`` () =
        TaskSeq.initAsync 1 (id >> Task.fromResult)
        |> TaskSeq.head
        |> Task.map (should equal 0)

    [<Fact>]
    let ``TaskSeq-init some values`` () =
        TaskSeq.init 42 (fun x -> x / 2)
        |> TaskSeq.length
        |> Task.map (should equal 42)

    [<Fact>]
    let ``TaskSeq-initAsync some values`` () =
        TaskSeq.init 42 (fun x -> Task.fromResult (x / 2))
        |> TaskSeq.length
        |> Task.map (should equal 42)

    [<Fact>]
    let ``TaskSeq-initInfinite`` () =
        TaskSeq.initInfinite (fun x -> x / 2)
        |> TaskSeq.item 1_000_001
        |> Task.map (should equal 500_000)

    [<Fact>]
    let ``TaskSeq-initInfiniteAsync`` () =
        TaskSeq.initInfiniteAsync (fun x -> Task.fromResult (x / 2))
        |> TaskSeq.item 1_000_001
        |> Task.map (should equal 500_000)

module SideEffects =
    let inc (i: int byref) =
        i <- i + 1
        i

    [<Fact>]
    let ``TaskSeq-init singleton with side effects`` () = task {
        let mutable x = 0

        let ts = TaskSeq.init 1 (fun _ -> inc &x)

        do! TaskSeq.head ts |> Task.map (should equal 1)
        do! TaskSeq.head ts |> Task.map (should equal 2)
        do! TaskSeq.head ts |> Task.map (should equal 3) // state mutates
    }

    [<Fact>]
    let ``TaskSeq-init singleton with side effects -- Current`` () = task {
        let mutable x = 0

        let ts = TaskSeq.init 1 (fun _ -> inc &x)

        let enumerator = ts.GetAsyncEnumerator()
        let! _ = enumerator.MoveNextAsync()
        do enumerator.Current |> should equal 1
        do enumerator.Current |> should equal 1
        do enumerator.Current |> should equal 1 // current state does not mutate
    }

    [<Fact>]
    let ``TaskSeq-initAsync singleton with side effects`` () = task {
        let mutable x = 0

        let ts = TaskSeq.initAsync 1 (fun _ -> Task.fromResult (inc &x))

        do! TaskSeq.head ts |> Task.map (should equal 1)
        do! TaskSeq.head ts |> Task.map (should equal 2)
        do! TaskSeq.head ts |> Task.map (should equal 3) // state mutates
    }

    [<Fact>]
    let ``TaskSeq-initAsync singleton with side effects -- Current`` () = task {
        let mutable x = 0

        let ts = TaskSeq.initAsync 1 (fun _ -> Task.fromResult (inc &x))

        let enumerator = ts.GetAsyncEnumerator()
        let! _ = enumerator.MoveNextAsync()
        do enumerator.Current |> should equal 1
        do enumerator.Current |> should equal 1
        do enumerator.Current |> should equal 1 // current state does not mutate
    }
