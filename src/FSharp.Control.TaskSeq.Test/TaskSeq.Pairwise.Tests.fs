module TaskSeq.Tests.Pairwise

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.pairwise
//


module EmptySeq =
    [<Fact>]
    let ``TaskSeq-pairwise with null source raises`` () = assertNullArg <| fun () -> TaskSeq.pairwise null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-pairwise on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.pairwise
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-pairwise on singleton returns empty`` () = taskSeq { yield 42 } |> TaskSeq.pairwise |> verifyEmpty

module Immutable =
    [<Fact>]
    let ``TaskSeq-pairwise on two elements returns one pair`` () = task {
        let! pairs =
            taskSeq { yield! [ 10; 20 ] }
            |> TaskSeq.pairwise
            |> TaskSeq.toListAsync

        pairs |> should equal [ (10, 20) ]
    }

    [<Fact>]
    let ``TaskSeq-pairwise returns consecutive overlapping pairs`` () = task {
        let! pairs =
            taskSeq { yield! [ 1..5 ] }
            |> TaskSeq.pairwise
            |> TaskSeq.toListAsync

        pairs |> should equal [ (1, 2); (2, 3); (3, 4); (4, 5) ]
    }

    [<Fact>]
    let ``TaskSeq-pairwise output length is source length minus one`` () = task {
        let! len =
            taskSeq { yield! [ 1..10 ] }
            |> TaskSeq.pairwise
            |> TaskSeq.length

        len |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-pairwise shares elements across adjacent pairs`` () = task {
        // element at index i is the right of pair i-1 and the left of pair i
        let! pairs =
            taskSeq { yield! [ 'A'; 'B'; 'C'; 'D' ] }
            |> TaskSeq.pairwise
            |> TaskSeq.toListAsync

        pairs |> should equal [ ('A', 'B'); ('B', 'C'); ('C', 'D') ]
        // check that middle elements appear in both adjacent pairs
        let (_, r0) = pairs[0]
        let (l1, _) = pairs[1]
        r0 |> should equal l1
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pairwise all variants - correct count and boundaries`` variant = task {
        // getSeqImmutable yields 1..10 → 9 pairs (1,2)..(9,10)
        let! pairs =
            Gen.getSeqImmutable variant
            |> TaskSeq.pairwise
            |> TaskSeq.toListAsync

        pairs |> List.length |> should equal 9
        pairs |> List.head |> should equal (1, 2)
        pairs |> List.last |> should equal (9, 10)
    }

module SideEffects =
    [<Fact>]
    let ``TaskSeq-pairwise consumes every source element exactly once`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..5 do
                count <- count + 1
                yield i
        }

        let! pairs = ts |> TaskSeq.pairwise |> TaskSeq.toListAsync
        count |> should equal 5
        pairs |> should equal [ (1, 2); (2, 3); (3, 4); (4, 5) ]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-pairwise on side-effect seq yields correct pairs`` variant = task {
        // getSeqWithSideEffect yields 1..10 on first iteration
        let! pairs =
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.pairwise
            |> TaskSeq.toListAsync

        pairs |> List.length |> should equal 9
        pairs |> List.head |> should equal (1, 2)
        pairs |> List.last |> should equal (9, 10)
    }
