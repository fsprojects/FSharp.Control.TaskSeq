module TaskSeq.Tests.CompareWith

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.compareWith
// TaskSeq.compareWithAsync
//

let inline sign x =
    if x < 0 then -1
    elif x > 0 then 1
    else 0

module EmptySeq =
    [<Fact>]
    let ``Null source1 is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.compareWith compare null TaskSeq.empty<int>

        assertNullArg
        <| fun () -> TaskSeq.compareWithAsync (fun a b -> Task.fromResult (compare a b)) null TaskSeq.empty<int>

    [<Fact>]
    let ``Null source2 is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.compareWith compare TaskSeq.empty<int> null

        assertNullArg
        <| fun () -> TaskSeq.compareWithAsync (fun a b -> Task.fromResult (compare a b)) TaskSeq.empty<int> null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-compareWith of two empty sequences is 0`` variant = task {
        let empty = Gen.getEmptyVariant variant
        let! result = TaskSeq.compareWith compare empty TaskSeq.empty<int>
        result |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-compareWithAsync of two empty sequences is 0`` variant = task {
        let empty = Gen.getEmptyVariant variant
        let! result = TaskSeq.compareWithAsync (fun a b -> Task.fromResult (compare a b)) empty TaskSeq.empty<int>
        result |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-compareWith: empty source1 is less than non-empty source2`` variant = task {
        let empty = Gen.getEmptyVariant variant
        let! result = TaskSeq.compareWith compare empty (TaskSeq.singleton 1)
        result |> should equal -1
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-compareWith: non-empty source1 is greater than empty source2`` variant = task {
        let empty = Gen.getEmptyVariant variant
        let! result = TaskSeq.compareWith compare (TaskSeq.singleton 1) empty
        result |> should equal 1
    }


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-compareWith: equal sequences return 0`` variant = task {
        let src1 = Gen.getSeqImmutable variant
        let src2 = Gen.getSeqImmutable variant
        let! result = TaskSeq.compareWith compare src1 src2
        result |> should equal 0
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-compareWithAsync: equal sequences return 0`` variant = task {
        let src1 = Gen.getSeqImmutable variant
        let src2 = Gen.getSeqImmutable variant
        let! result = TaskSeq.compareWithAsync (fun a b -> Task.fromResult (compare a b)) src1 src2
        result |> should equal 0
    }

    [<Fact>]
    let ``TaskSeq-compareWith: first element differs`` () = task {
        let src1 = taskSeq {
            1
            2
            3
        }

        let src2 = taskSeq {
            2
            2
            3
        }

        let! result = TaskSeq.compareWith compare src1 src2
        sign result |> should equal -1
    }

    [<Fact>]
    let ``TaskSeq-compareWith: last element differs`` () = task {
        let src1 = taskSeq {
            1
            2
            4
        }

        let src2 = taskSeq {
            1
            2
            3
        }

        let! result = TaskSeq.compareWith compare src1 src2
        sign result |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-compareWith: source1 shorter returns negative`` () = task {
        let src1 = taskSeq {
            1
            2
        }

        let src2 = taskSeq {
            1
            2
            3
        }

        let! result = TaskSeq.compareWith compare src1 src2
        result |> should equal -1
    }

    [<Fact>]
    let ``TaskSeq-compareWith: source2 shorter returns positive`` () = task {
        let src1 = taskSeq {
            1
            2
            3
        }

        let src2 = taskSeq {
            1
            2
        }

        let! result = TaskSeq.compareWith compare src1 src2
        result |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-compareWith: uses custom comparer result sign`` () = task {
        // comparer returns a large number, not just -1/0/1
        let bigCompare (a: int) (b: int) = (a - b) * 100

        let src1 = taskSeq {
            1
            2
            3
        }

        let src2 = taskSeq {
            1
            2
            5
        }

        let! result = TaskSeq.compareWith bigCompare src1 src2
        // 3 compared to 5 gives (3-5)*100 = -200, which is negative
        result |> should be (lessThan 0)
    }

    [<Fact>]
    let ``TaskSeq-compareWith: stops at first non-zero comparison`` () = task {
        let mutable callCount = 0

        let countingCompare a b =
            callCount <- callCount + 1
            compare a b

        let src1 = taskSeq {
            1
            99
            99
            99
        }

        let src2 = taskSeq {
            2
            99
            99
            99
        }

        let! result = TaskSeq.compareWith countingCompare src1 src2
        sign result |> should equal -1
        // Should stop after first comparison
        callCount |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-compareWithAsync: async comparer works`` () = task {
        let src1 = taskSeq {
            1
            2
            3
        }

        let src2 = taskSeq {
            1
            2
            4
        }

        let! result =
            TaskSeq.compareWithAsync
                (fun a b -> task {
                    // simulate async work with a yield point
                    return compare a b
                })
                src1
                src2

        sign result |> should equal -1
    }

    [<Fact>]
    let ``TaskSeq-compareWithAsync: async comparer works correctly`` () = task {
        let src1 = taskSeq {
            10
            20
            30
        }

        let src2 = taskSeq {
            10
            20
            30
        }

        let! result = TaskSeq.compareWithAsync (fun a b -> Task.fromResult (compare a b)) src1 src2
        result |> should equal 0
    }

module SideEffects =
    [<Fact>]
    let ``TaskSeq-compareWith consumes both sequences exactly once when equal`` () = task {
        let mutable count1 = 0
        let mutable count2 = 0

        let src1 = taskSeq {
            for i in 1..5 do
                count1 <- count1 + 1
                yield i
        }

        let src2 = taskSeq {
            for i in 1..5 do
                count2 <- count2 + 1
                yield i
        }

        let! result = TaskSeq.compareWith compare src1 src2
        result |> should equal 0
        count1 |> should equal 5
        count2 |> should equal 5
    }

    [<Fact>]
    let ``TaskSeq-compareWith stops consuming sources after first non-zero comparison`` () = task {
        let mutable count1 = 0
        let mutable count2 = 0

        let src1 = taskSeq {
            for i in [ 1; 99; 99; 99; 99 ] do
                count1 <- count1 + 1
                yield i
        }

        let src2 = taskSeq {
            for i in [ 2; 99; 99; 99; 99 ] do
                count2 <- count2 + 1
                yield i
        }

        let! result = TaskSeq.compareWith compare src1 src2
        sign result |> should equal -1
        // compareWith calls MoveNextAsync for the first element of each source before entering the loop;
        // when the first comparison is non-zero the loop exits immediately without advancing further.
        count1 |> should equal 1
        count2 |> should equal 1
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-compareWith two fresh side-effect sequences compare as equal`` variant = task {
        // Each call to getSeqWithSideEffect creates an independent counter starting at 0,
        // so both sequences yield 1..10 and should compare as equal.
        let src1 = Gen.getSeqWithSideEffect variant
        let src2 = Gen.getSeqWithSideEffect variant
        let! result = TaskSeq.compareWith compare src1 src2
        result |> should equal 0
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-compareWithAsync two fresh side-effect sequences compare as equal`` variant = task {
        let src1 = Gen.getSeqWithSideEffect variant
        let src2 = Gen.getSeqWithSideEffect variant
        let! result = TaskSeq.compareWithAsync (fun a b -> Task.fromResult (compare a b)) src1 src2
        result |> should equal 0
    }
