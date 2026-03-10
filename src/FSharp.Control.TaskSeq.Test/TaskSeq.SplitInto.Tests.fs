module TaskSeq.Tests.SplitInto

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.splitInto
//

module EmptySeq =
    [<Fact>]
    let ``TaskSeq-splitInto with null source raises`` () = assertNullArg <| fun () -> TaskSeq.splitInto 1 null

    [<Fact>]
    let ``TaskSeq-splitInto with zero raises ArgumentException before awaiting`` () =
        fun () -> TaskSeq.empty<int> |> TaskSeq.splitInto 0 |> ignore // throws eagerly, before enumeration
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``TaskSeq-splitInto with negative raises ArgumentException before awaiting`` () =
        fun () -> TaskSeq.empty<int> |> TaskSeq.splitInto -1 |> ignore
        |> should throw typeof<System.ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-splitInto on empty sequence yields empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.splitInto 1
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-splitInto(99) on empty sequence yields empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.splitInto 99
        |> verifyEmpty

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitInto preserves all elements in order`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.splitInto 3
            |> TaskSeq.collect TaskSeq.ofArray
            |> verify1To10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitInto(2) splits 10-element sequence into 2 chunks of 5`` variant = task {
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.splitInto 2
            |> TaskSeq.toArrayAsync

        chunks |> should equal [| [| 1..5 |]; [| 6..10 |] |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitInto(5) splits 10-element sequence into 5 chunks of 2`` variant = task {
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.splitInto 5
            |> TaskSeq.toArrayAsync

        chunks
        |> should equal [| [| 1; 2 |]; [| 3; 4 |]; [| 5; 6 |]; [| 7; 8 |]; [| 9; 10 |] |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitInto(10) splits 10-element sequence into 10 singleton chunks`` variant = task {
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.splitInto 10
            |> TaskSeq.toArrayAsync

        chunks |> Array.length |> should equal 10

        chunks
        |> Array.iteri (fun i chunk -> chunk |> should equal [| i + 1 |])
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitInto(1) returns the whole sequence as one chunk`` variant = task {
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.splitInto 1
            |> TaskSeq.toArrayAsync

        chunks |> should equal [| [| 1..10 |] |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitInto(3) distributes remainder across first chunks`` variant = task {
        // 10 elements into 3 chunks: 10 / 3 = 3 remainder 1 → [4; 3; 3]
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.splitInto 3
            |> TaskSeq.toArrayAsync

        chunks |> Array.length |> should equal 3
        chunks.[0] |> should equal [| 1; 2; 3; 4 |]
        chunks.[1] |> should equal [| 5; 6; 7 |]
        chunks.[2] |> should equal [| 8; 9; 10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitInto(4) distributes remainder across first chunks`` variant = task {
        // 10 elements into 4 chunks: 10 / 4 = 2 remainder 2 → [3; 3; 2; 2]
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.splitInto 4
            |> TaskSeq.toArrayAsync

        chunks |> Array.length |> should equal 4
        chunks.[0] |> should equal [| 1; 2; 3 |]
        chunks.[1] |> should equal [| 4; 5; 6 |]
        chunks.[2] |> should equal [| 7; 8 |]
        chunks.[3] |> should equal [| 9; 10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitInto count greater than length returns one element per chunk`` variant = task {
        // 10 elements into 20 chunks → 10 singleton chunks
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.splitInto 20
            |> TaskSeq.toArrayAsync

        chunks |> Array.length |> should equal 10

        chunks
        |> Array.iteri (fun i chunk -> chunk |> should equal [| i + 1 |])
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-splitInto preserves all side-effectful elements in order`` variant = task {
        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.splitInto 3
            |> TaskSeq.collect TaskSeq.ofArray
            |> verify1To10
    }
