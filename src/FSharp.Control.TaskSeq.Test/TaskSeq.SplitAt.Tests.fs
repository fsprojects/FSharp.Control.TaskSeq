module TaskSeq.Tests.SplitAt

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.splitAt
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () = assertNullArg <| fun () -> TaskSeq.splitAt 0 null

    [<Fact>]
    let ``Negative count raises immediately`` () =
        fun () -> TaskSeq.splitAt -1 (taskSeq { yield 1 }) |> Task.ignore

        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-splitAt 0 on empty gives empty prefix and empty rest`` variant = task {
        let! prefix, rest = Gen.getEmptyVariant variant |> TaskSeq.splitAt 0
        prefix |> should haveLength 0
        do! verifyEmpty rest
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-splitAt n on empty gives empty prefix and empty rest`` variant = task {
        let! prefix, rest = Gen.getEmptyVariant variant |> TaskSeq.splitAt 5
        prefix |> should haveLength 0
        do! verifyEmpty rest
    }


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitAt 0 returns empty prefix and full rest`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! prefix, rest = TaskSeq.splitAt 0 ts
        prefix |> should haveLength 0
        let! restArr = TaskSeq.toArrayAsync rest
        restArr |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitAt at length gives full prefix and empty rest`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! prefix, rest = TaskSeq.splitAt 10 ts
        prefix |> should equal [| 1..10 |]
        do! verifyEmpty rest
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitAt in the middle`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! prefix, rest = TaskSeq.splitAt 3 ts
        prefix |> should equal [| 1; 2; 3 |]
        let! restArr = TaskSeq.toArrayAsync rest
        restArr |> should equal [| 4..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-splitAt 1 returns singleton prefix`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! prefix, rest = TaskSeq.splitAt 1 ts
        prefix |> should equal [| 1 |]
        let! restArr = TaskSeq.toArrayAsync rest
        restArr |> should equal [| 2..10 |]
    }

    [<Fact>]
    let ``TaskSeq-splitAt beyond length gives full prefix and empty rest`` () = task {
        let ts = taskSeq {
            yield 1
            yield 2
            yield 3
        }

        let! prefix, rest = TaskSeq.splitAt 100 ts
        prefix |> should equal [| 1; 2; 3 |]
        do! verifyEmpty rest
    }

    [<Fact>]
    let ``TaskSeq-splitAt prefix and rest together contain all elements`` () = task {
        let data = [| 1..20 |]
        let ts = TaskSeq.ofArray data
        let splitPoint = 7
        let! prefix, rest = TaskSeq.splitAt splitPoint ts
        let! restArr = TaskSeq.toArrayAsync rest
        let combined = Array.append prefix restArr
        combined |> should equal data
    }
