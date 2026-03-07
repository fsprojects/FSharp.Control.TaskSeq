module TaskSeq.Tests.Indexed

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.indexed
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () = assertNullArg <| fun () -> TaskSeq.indexed null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-indexed on empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.indexed
        |> verifyEmpty

module Immutable =
    [<Fact>]
    let ``TaskSeq-indexed starts at zero`` () =
        taskSeq { yield 99 }
        |> TaskSeq.indexed
        |> TaskSeq.head
        |> Task.map (should equal (0, 99))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-indexed`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.indexed
        |> TaskSeq.toArrayAsync
        |> Task.map (Array.forall (fun (x, y) -> x + 1 = y))
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-indexed returns all 10 pairs with correct zero-based indices`` variant = task {
        let! pairs =
            Gen.getSeqImmutable variant
            |> TaskSeq.indexed
            |> TaskSeq.toArrayAsync

        pairs |> should be (haveLength 10)

        pairs
        |> Array.iteri (fun pos (idx, _) -> idx |> should equal pos)
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-indexed returns values 1 to 10 unchanged`` variant = task {
        let! pairs =
            Gen.getSeqImmutable variant
            |> TaskSeq.indexed
            |> TaskSeq.toArrayAsync

        pairs |> Array.map snd |> should equal [| 1..10 |]
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-indexed on side-effect sequence returns correct pairs`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! pairs = ts |> TaskSeq.indexed |> TaskSeq.toArrayAsync
        pairs |> should be (haveLength 10)

        pairs
        |> Array.iteri (fun pos (idx, _) -> idx |> should equal pos)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-indexed on side-effect sequence is re-evaluated on second iteration`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! firstPairs = ts |> TaskSeq.indexed |> TaskSeq.toArrayAsync
        let! secondPairs = ts |> TaskSeq.indexed |> TaskSeq.toArrayAsync

        // indices always start at 0
        firstPairs |> Array.map fst |> should equal [| 0..9 |]
        secondPairs |> Array.map fst |> should equal [| 0..9 |]

        // values advance due to side effects
        firstPairs |> Array.map snd |> should equal [| 1..10 |]
        secondPairs |> Array.map snd |> should equal [| 11..20 |]
    }

    [<Fact>]
    let ``TaskSeq-indexed prove index starts at zero even after side effects`` () = task {
        let mutable counter = 0

        let ts = taskSeq {
            for _ in 1..5 do
                counter <- counter + 1
                yield counter
        }

        let! pairs = ts |> TaskSeq.indexed |> TaskSeq.toArrayAsync
        pairs |> Array.map fst |> should equal [| 0..4 |]
        pairs |> Array.map snd |> should equal [| 1..5 |]
    }
