module TaskSeq.Tests.AllPairs

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.allPairs
//

module EmptySeq =
    [<Fact>]
    let ``TaskSeq-allPairs with null source1 raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.allPairs null TaskSeq.empty<int>

    [<Fact>]
    let ``TaskSeq-allPairs with null source2 raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.allPairs TaskSeq.empty<int> null

    [<Fact>]
    let ``TaskSeq-allPairs with both null raises`` () = assertNullArg <| fun () -> TaskSeq.allPairs null null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-allPairs returns empty when source1 is empty`` variant = task {
        let! result =
            TaskSeq.allPairs (Gen.getEmptyVariant variant) (taskSeq { yield! [ 1..5 ] })
            |> TaskSeq.toArrayAsync

        result |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-allPairs returns empty when source2 is empty`` variant = task {
        let! result =
            TaskSeq.allPairs (taskSeq { yield! [ 1..5 ] }) (Gen.getEmptyVariant variant)
            |> TaskSeq.toArrayAsync

        result |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-allPairs returns empty when both sources are empty`` variant = task {
        let! result =
            TaskSeq.allPairs (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
            |> TaskSeq.toArrayAsync

        result |> should be Empty
    }


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-allPairs produces all pairs in row-major order`` variant = task {
        let! result =
            TaskSeq.allPairs (Gen.getSeqImmutable variant) (taskSeq { yield! [ 'a'; 'b'; 'c' ] })
            |> TaskSeq.toArrayAsync

        // source1 has 10 elements (1..10), source2 has 3 chars → 30 pairs
        result |> should be (haveLength 30)

        // check that for each element of source1, all elements of source2 appear consecutively
        for i in 0..9 do
            result.[i * 3 + 0] |> should equal (i + 1, 'a')
            result.[i * 3 + 1] |> should equal (i + 1, 'b')
            result.[i * 3 + 2] |> should equal (i + 1, 'c')
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-allPairs with singleton source1 gives one pair per source2 element`` variant = task {
        let! result =
            TaskSeq.allPairs (TaskSeq.singleton 42) (Gen.getSeqImmutable variant)
            |> TaskSeq.toArrayAsync

        result |> should be (haveLength 10)
        result |> should equal (Array.init 10 (fun i -> 42, i + 1))
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-allPairs with singleton source2 gives one pair per source1 element`` variant = task {
        let! result =
            TaskSeq.allPairs (Gen.getSeqImmutable variant) (TaskSeq.singleton 42)
            |> TaskSeq.toArrayAsync

        result |> should be (haveLength 10)
        result |> should equal (Array.init 10 (fun i -> i + 1, 42))
    }

    [<Fact>]
    let ``TaskSeq-allPairs matches Seq.allPairs for small sequences`` () = task {
        let xs = [ 1; 2; 3 ]
        let ys = [ 10; 20 ]

        let expected = Seq.allPairs xs ys |> Seq.toArray

        let! result =
            TaskSeq.allPairs (TaskSeq.ofList xs) (TaskSeq.ofList ys)
            |> TaskSeq.toArrayAsync

        result |> should equal expected
    }

    [<Fact>]
    let ``TaskSeq-allPairs source2 is fully materialised before source1 is iterated`` () = task {
        // Verify: if source2 raises, it raises before any element of the result is consumed.
        let source2 = taskSeq {
            yield 1
            failwith "source2 error"
        }

        let result = TaskSeq.allPairs (TaskSeq.ofList [ 'a'; 'b' ]) source2

        // Consuming even the first element should surface the exception from source2
        do! task {
            try
                let! _ = TaskSeq.head result
                failwith "expected exception"
            with ex ->
                ex.Message |> should equal "source2 error"
        }
    }


module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-allPairs works with side-effect source1`` variant = task {
        let! result =
            TaskSeq.allPairs (Gen.getSeqWithSideEffect variant) (taskSeq { yield! [ 'x'; 'y' ] })
            |> TaskSeq.toArrayAsync

        result |> should be (haveLength 20)

        for i in 0..9 do
            result.[i * 2 + 0] |> should equal (i + 1, 'x')
            result.[i * 2 + 1] |> should equal (i + 1, 'y')
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-allPairs works with side-effect source2`` variant = task {
        let! result =
            TaskSeq.allPairs (taskSeq { yield! [ 'x'; 'y' ] }) (Gen.getSeqWithSideEffect variant)
            |> TaskSeq.toArrayAsync

        result |> should be (haveLength 20)

        for i in 0..1 do
            for j in 0..9 do
                result.[i * 10 + j]
                |> should equal ((if i = 0 then 'x' else 'y'), j + 1)
    }
