module TaskSeq.Tests.Filter

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.filter
// TaskSeq.filterAsync
// TaskSeq.where
// TaskSeq.whereAsync
//


module EmptySeq =
    [<Fact>]
    let ``TaskSeq-filter or where with null source raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.filter (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.filterAsync (fun _ -> Task.fromResult false) null

        assertNullArg
        <| fun () -> TaskSeq.where (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.whereAsync (fun _ -> Task.fromResult false) null


    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-filter or where has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.filter ((=) 12)
            |> TaskSeq.toListAsync
            |> Task.map (List.isEmpty >> should be True)

        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.where ((=) 12)
            |> TaskSeq.toListAsync
            |> Task.map (List.isEmpty >> should be True)
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-filterAsync or whereAsync has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.filterAsync (fun x -> task { return x = 12 })
            |> TaskSeq.toListAsync
            |> Task.map (List.isEmpty >> should be True)

        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.whereAsync (fun x -> task { return x = 12 })
            |> TaskSeq.toListAsync
            |> Task.map (List.isEmpty >> should be True)
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filter or where filters correctly`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.filter ((<=) 5) // greater than
            |> verifyDigitsAsString "EFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.where ((>) 5) // greater than
            |> verifyDigitsAsString "ABCD"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filterAsync or whereAsync filters correctly`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.filterAsync (fun x -> task { return x <= 5 })
            |> verifyDigitsAsString "ABCDE"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.whereAsync (fun x -> task { return x > 5 })
            |> verifyDigitsAsString "FGHIJ"

    }

module Immutable2 =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filter keeps all when predicate is always true`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.filter (fun _ -> true)
        |> verify1To10

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filterAsync keeps all when predicate is always true`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.filterAsync (fun _ -> Task.fromResult true)
        |> verify1To10

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filter returns empty when predicate is always false`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.filter (fun _ -> false)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filterAsync returns empty when predicate is always false`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.filterAsync (fun _ -> Task.fromResult false)
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-filter evaluates each element exactly once`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..5 do
                count <- count + 1
                yield i
        }

        let! xs =
            ts
            |> TaskSeq.filter (fun x -> x % 2 = 0)
            |> TaskSeq.toListAsync

        count |> should equal 5
        xs |> should equal [ 2; 4 ]
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-filter filters correctly`` variant = task {
        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.filter ((<=) 5) // greater than or equal
            |> verifyDigitsAsString "EFGHIJ"

        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.where ((>) 5) // less than
            |> verifyDigitsAsString "ABCD"
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-filterAsync filters correctly`` variant = task {
        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.filterAsync (fun x -> task { return x <= 5 })
            |> verifyDigitsAsString "ABCDE"

        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.whereAsync (fun x -> task { return x > 5 && x < 9 })
            |> verifyDigitsAsString "FGH"
    }
