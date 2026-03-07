module TaskSeq.Tests.DistinctUntilChanged

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.distinctUntilChanged
//


module EmptySeq =
    [<Fact>]
    let ``TaskSeq-distinctUntilChanged with null source raises`` () = assertNullArg <| fun () -> TaskSeq.distinctUntilChanged null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-distinctUntilChanged has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.distinctUntilChanged
            |> TaskSeq.toListAsync
            |> Task.map (List.isEmpty >> should be True)
    }

module Functionality =
    [<Fact>]
    let ``TaskSeq-distinctUntilChanged should return no consecutive duplicates`` () = task {
        let ts =
            [ 'A'; 'A'; 'B'; 'Z'; 'C'; 'C'; 'Z'; 'C'; 'D'; 'D'; 'D'; 'Z' ]
            |> TaskSeq.ofList

        let! xs = ts |> TaskSeq.distinctUntilChanged |> TaskSeq.toListAsync

        xs
        |> List.map string
        |> String.concat ""
        |> should equal "ABZCZCDZ"
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChanged with single element returns singleton`` () = task {
        let! xs =
            taskSeq { yield 42 }
            |> TaskSeq.distinctUntilChanged
            |> TaskSeq.toListAsync

        xs |> should equal [ 42 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChanged with all identical elements returns one element`` () = task {
        let! xs =
            taskSeq { yield! [ 7; 7; 7; 7; 7 ] }
            |> TaskSeq.distinctUntilChanged
            |> TaskSeq.toListAsync

        xs |> should equal [ 7 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChanged with all distinct elements returns all`` () = task {
        let! xs =
            taskSeq { yield! [ 1; 2; 3; 4; 5 ] }
            |> TaskSeq.distinctUntilChanged
            |> TaskSeq.toListAsync

        xs |> should equal [ 1; 2; 3; 4; 5 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChanged with alternating pairs`` () = task {
        // [A;A;B;B;A;A] -> [A;B;A]
        let! xs =
            taskSeq { yield! [ 'A'; 'A'; 'B'; 'B'; 'A'; 'A' ] }
            |> TaskSeq.distinctUntilChanged
            |> TaskSeq.toListAsync

        xs |> should equal [ 'A'; 'B'; 'A' ]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-distinctUntilChanged on immutable all-unique seq preserves all elements`` variant = task {
        // getSeqImmutable yields 1..10, all unique, so all are returned
        let! xs =
            Gen.getSeqImmutable variant
            |> TaskSeq.distinctUntilChanged
            |> TaskSeq.toListAsync

        xs |> should equal [ 1..10 ]
    }

module SideEffects =
    [<Fact>]
    let ``TaskSeq-distinctUntilChanged consumes every element exactly once`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..6 do
                count <- count + 1
                yield i % 3 // yields 1,2,0,1,2,0 — no consecutive duplicates
        }

        let! xs = ts |> TaskSeq.distinctUntilChanged |> TaskSeq.toListAsync
        count |> should equal 6
        xs |> should equal [ 1; 2; 0; 1; 2; 0 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChanged skips duplicates without extra evaluation`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in [ 1; 1; 2; 2; 3 ] do
                count <- count + 1
                yield i
        }

        let! xs = ts |> TaskSeq.distinctUntilChanged |> TaskSeq.toListAsync
        // All 5 source elements must still be consumed
        count |> should equal 5
        xs |> should equal [ 1; 2; 3 ]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-distinctUntilChanged on side-effect seq preserves all unique elements`` variant = task {
        // getSeqWithSideEffect yields 1..10 (all unique on first iteration)
        let! xs =
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.distinctUntilChanged
            |> TaskSeq.toListAsync

        xs |> should equal [ 1..10 ]
    }
