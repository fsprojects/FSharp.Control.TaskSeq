module TaskSeq.Tests.GroupBy

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.groupBy
// TaskSeq.groupByAsync
// TaskSeq.countBy
// TaskSeq.countByAsync
// TaskSeq.partition
// TaskSeq.partitionAsync
//

module EmptySeq =
    [<Fact>]
    let ``TaskSeq-groupBy with null source raises`` () =
        assertNullArg <| fun () -> TaskSeq.groupBy id null

        assertNullArg
        <| fun () -> TaskSeq.groupByAsync (fun x -> Task.fromResult x) null

    [<Fact>]
    let ``TaskSeq-countBy with null source raises`` () =
        assertNullArg <| fun () -> TaskSeq.countBy id null

        assertNullArg
        <| fun () -> TaskSeq.countByAsync (fun x -> Task.fromResult x) null

    [<Fact>]
    let ``TaskSeq-partition with null source raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.partition (fun _ -> true) null

        assertNullArg
        <| fun () -> TaskSeq.partitionAsync (fun _ -> Task.fromResult true) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-groupBy on empty sequence returns empty array`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.groupBy (fun x -> x % 2)

        result |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-groupByAsync on empty sequence returns empty array`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.groupByAsync (fun x -> task { return x % 2 })

        result |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-countBy on empty sequence returns empty array`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.countBy (fun x -> x % 2)

        result |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-countByAsync on empty sequence returns empty array`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.countByAsync (fun x -> task { return x % 2 })

        result |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-partition on empty sequence returns two empty arrays`` variant = task {
        let! trueItems, falseItems =
            Gen.getEmptyVariant variant
            |> TaskSeq.partition (fun _ -> true)

        trueItems |> should be Empty
        falseItems |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-partitionAsync on empty sequence returns two empty arrays`` variant = task {
        let! trueItems, falseItems =
            Gen.getEmptyVariant variant
            |> TaskSeq.partitionAsync (fun _ -> Task.fromResult true)

        trueItems |> should be Empty
        falseItems |> should be Empty
    }


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-groupBy groups by even/odd`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.groupBy (fun x -> x % 2 = 0)

        // should have exactly two groups
        result |> Array.length |> should equal 2

        let falseKey, oddItems = result[0] // 1 is first, so 'false' (odd) comes first
        let trueKey, evenItems = result[1]
        falseKey |> should equal false
        trueKey |> should equal true
        oddItems |> should equal [| 1; 3; 5; 7; 9 |]
        evenItems |> should equal [| 2; 4; 6; 8; 10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-groupByAsync groups by even/odd`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.groupByAsync (fun x -> task { return x % 2 = 0 })

        result |> Array.length |> should equal 2

        let falseKey, oddItems = result[0]
        let trueKey, evenItems = result[1]
        falseKey |> should equal false
        trueKey |> should equal true
        oddItems |> should equal [| 1; 3; 5; 7; 9 |]
        evenItems |> should equal [| 2; 4; 6; 8; 10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-groupBy with identity projection produces one group per element`` variant = task {
        let! result = Gen.getSeqImmutable variant |> TaskSeq.groupBy id

        result |> Array.length |> should equal 10

        for key, items in result do
            items |> should equal [| key |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-groupBy preserves first-occurrence key ordering`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.groupBy (fun x -> x % 3)

        // 1 % 3 = 1 → first key is 1
        // 2 % 3 = 2 → second key is 2
        // 3 % 3 = 0 → third key is 0
        let keys = result |> Array.map fst
        keys |> should equal [| 1; 2; 0 |]

        let _, group1 = result[0] // remainder 1: 1, 4, 7, 10
        let _, group2 = result[1] // remainder 2: 2, 5, 8
        let _, group0 = result[2] // remainder 0: 3, 6, 9
        group1 |> should equal [| 1; 4; 7; 10 |]
        group2 |> should equal [| 2; 5; 8 |]
        group0 |> should equal [| 3; 6; 9 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-groupBy with constant key produces single group`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.groupBy (fun _ -> "same")

        result |> Array.length |> should equal 1
        let key, items = result[0]
        key |> should equal "same"
        items |> should equal [| 1; 2; 3; 4; 5; 6; 7; 8; 9; 10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-countBy counts by even/odd`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.countBy (fun x -> x % 2 = 0)

        result |> Array.length |> should equal 2

        let falseKey, oddCount = result[0]
        let trueKey, evenCount = result[1]
        falseKey |> should equal false
        trueKey |> should equal true
        oddCount |> should equal 5
        evenCount |> should equal 5
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-countByAsync counts by even/odd`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.countByAsync (fun x -> task { return x % 2 = 0 })

        result |> Array.length |> should equal 2

        let falseKey, oddCount = result[0]
        let trueKey, evenCount = result[1]
        falseKey |> should equal false
        trueKey |> should equal true
        oddCount |> should equal 5
        evenCount |> should equal 5
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-countBy preserves first-occurrence key ordering`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.countBy (fun x -> x % 3)

        let keys = result |> Array.map fst
        keys |> should equal [| 1; 2; 0 |]

        let _, count1 = result[0] // remainder 1: 1, 4, 7, 10 → 4 items
        let _, count2 = result[1] // remainder 2: 2, 5, 8 → 3 items
        let _, count0 = result[2] // remainder 0: 3, 6, 9 → 3 items
        count1 |> should equal 4
        count2 |> should equal 3
        count0 |> should equal 3
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-countBy with constant key counts all`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.countBy (fun _ -> "same")

        result |> should equal [| "same", 10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-partition splits by even`` variant = task {
        let! evens, odds =
            Gen.getSeqImmutable variant
            |> TaskSeq.partition (fun x -> x % 2 = 0)

        evens |> should equal [| 2; 4; 6; 8; 10 |]
        odds |> should equal [| 1; 3; 5; 7; 9 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-partitionAsync splits by even`` variant = task {
        let! evens, odds =
            Gen.getSeqImmutable variant
            |> TaskSeq.partitionAsync (fun x -> task { return x % 2 = 0 })

        evens |> should equal [| 2; 4; 6; 8; 10 |]
        odds |> should equal [| 1; 3; 5; 7; 9 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-partition with always-true predicate puts all in first array`` variant = task {
        let! trueItems, falseItems =
            Gen.getSeqImmutable variant
            |> TaskSeq.partition (fun _ -> true)

        trueItems
        |> should equal [| 1; 2; 3; 4; 5; 6; 7; 8; 9; 10 |]

        falseItems |> should be Empty
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-partition with always-false predicate puts all in second array`` variant = task {
        let! trueItems, falseItems =
            Gen.getSeqImmutable variant
            |> TaskSeq.partition (fun _ -> false)

        trueItems |> should be Empty

        falseItems
        |> should equal [| 1; 2; 3; 4; 5; 6; 7; 8; 9; 10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-partition preserves element order within each partition`` variant = task {
        let! trueItems, falseItems =
            Gen.getSeqImmutable variant
            |> TaskSeq.partition (fun x -> x <= 5)

        trueItems |> should equal [| 1; 2; 3; 4; 5 |]
        falseItems |> should equal [| 6; 7; 8; 9; 10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-partitionAsync preserves element order within each partition`` variant = task {
        let! trueItems, falseItems =
            Gen.getSeqImmutable variant
            |> TaskSeq.partitionAsync (fun x -> task { return x <= 5 })

        trueItems |> should equal [| 1; 2; 3; 4; 5 |]
        falseItems |> should equal [| 6; 7; 8; 9; 10 |]
    }


module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-groupBy groups side-effecting sequence`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! result = ts |> TaskSeq.groupBy (fun x -> x % 2 = 0)

        result |> Array.length |> should equal 2
        // re-evaluating yields new side-effects (next 10 items: 11..20)
        let! result2 = ts |> TaskSeq.groupBy (fun x -> x % 2 = 0)
        result2 |> Array.length |> should equal 2
        let _, group2 = result2[0]
        group2 |> Array.sum |> should equal (11 + 13 + 15 + 17 + 19) // odd items from 11–20
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-countBy counts side-effecting sequence`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! result = ts |> TaskSeq.countBy (fun x -> x % 2 = 0)

        // 5 odd, 5 even from 1..10
        let falseKey, oddCount = result[0]
        let trueKey, evenCount = result[1]
        falseKey |> should equal false
        trueKey |> should equal true
        oddCount |> should equal 5
        evenCount |> should equal 5
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-partition splits side-effecting sequence`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! evens, odds = ts |> TaskSeq.partition (fun x -> x % 2 = 0)

        evens |> should equal [| 2; 4; 6; 8; 10 |]
        odds |> should equal [| 1; 3; 5; 7; 9 |]

        // second call picks up side effects
        let! evens2, odds2 = ts |> TaskSeq.partition (fun x -> x % 2 = 0)
        evens2 |> should equal [| 12; 14; 16; 18; 20 |]
        odds2 |> should equal [| 11; 13; 15; 17; 19 |]
    }
