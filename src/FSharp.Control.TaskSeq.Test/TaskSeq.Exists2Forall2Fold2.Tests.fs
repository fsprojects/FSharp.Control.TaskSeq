module TaskSeq.Tests.Exists2Forall2Fold2

open System.Text

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.exists2
// TaskSeq.forall2
// TaskSeq.forall2Async
// TaskSeq.fold2
// TaskSeq.fold2Async
//


///////////
// exists2
///////////

module Exists2EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.exists2 (fun _ _ -> false) null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.exists2 (fun _ _ -> false) TaskSeq.empty null

        assertNullArg
        <| fun () -> TaskSeq.exists2 (fun _ _ -> false) null null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-exists2 returns false when both sources are empty`` variant =
        TaskSeq.exists2 (fun _ _ -> true) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-exists2 returns false when first source is empty`` variant =
        TaskSeq.exists2 (fun _ _ -> true) (Gen.getEmptyVariant variant) (TaskSeq.ofList [ 1; 2; 3 ])
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-exists2 returns false when second source is empty`` variant =
        TaskSeq.exists2 (fun _ _ -> true) (TaskSeq.ofList [ 1; 2; 3 ]) (Gen.getEmptyVariant variant)
        |> Task.map (should be False)


module Exists2Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exists2 sad path returns false when no pair matches`` variant =
        TaskSeq.exists2 (fun x y -> x = y && x > 100) (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exists2 happy path returns true when first pair matches`` variant =
        // source1 and source2 are both 1..10; predicate (=) matches every pair
        TaskSeq.exists2 (=) (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exists2 happy path finds pair in middle of seq`` variant =
        // source1 = 1..10, source2 = 1..10; pair (5,5) satisfies the predicate
        TaskSeq.exists2 (fun x y -> x = 5 && y = 5) (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exists2 happy path finds pair at end of seq`` variant =
        TaskSeq.exists2 (fun x y -> x = 10 && y = 10) (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)
        |> Task.map (should be True)

    [<Fact>]
    let ``TaskSeq-exists2 stops at shorter sequence - first shorter`` () =
        // source1 = [1;2;3], source2 = [1;2;3;100;200]
        // predicate checks if sum > 50; without truncation, pairs (4,100)+(5,200) would match
        TaskSeq.exists2 (fun x y -> x + y > 50) (TaskSeq.ofList [ 1; 2; 3 ]) (TaskSeq.ofList [ 1; 2; 3; 100; 200 ])
        |> Task.map (should be False)

    [<Fact>]
    let ``TaskSeq-exists2 stops at shorter sequence - second shorter`` () =
        TaskSeq.exists2 (fun x y -> x + y > 50) (TaskSeq.ofList [ 1; 2; 3; 100; 200 ]) (TaskSeq.ofList [ 1; 2; 3 ])
        |> Task.map (should be False)

    [<Fact>]
    let ``TaskSeq-exists2 works with different element types`` () =
        TaskSeq.exists2 (fun (x: int) (y: string) -> string x = y) (TaskSeq.ofList [ 1; 2; 3 ]) (TaskSeq.ofList [ "1"; "2"; "3" ])
        |> Task.map (should be True)


module Exists2SideEffects =
    [<Fact>]
    let ``TaskSeq-exists2 _specialcase_ stops evaluating after first match`` () = task {
        let mutable i = 0
        let mutable j = 0

        let ts1 = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let ts2 = taskSeq {
            for _ in 0..9 do
                j <- j + 1
                yield j
        }

        // predicate matches on second pair (2, 2)
        let! found = TaskSeq.exists2 (fun x y -> x = 2 && y = 2) ts1 ts2
        found |> should be True
        i |> should equal 2 // only partial evaluation
        j |> should equal 2
    }

    [<Fact>]
    let ``TaskSeq-exists2 _specialcase_ evaluates all pairs when not found`` () = task {
        let mutable i = 0
        let mutable j = 0

        let ts1 = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let ts2 = taskSeq {
            for _ in 0..9 do
                j <- j + 1
                yield j
        }

        let! found = TaskSeq.exists2 (fun x y -> x = 999 && y = 999) ts1 ts2
        found |> should be False
        i |> should equal 10
        j |> should equal 10
    }


///////////
// forall2
///////////

module Forall2EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.forall2 (fun _ _ -> true) null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.forall2 (fun _ _ -> true) TaskSeq.empty null

        assertNullArg
        <| fun () -> TaskSeq.forall2Async (fun _ _ -> Task.fromResult true) null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.forall2Async (fun _ _ -> Task.fromResult true) TaskSeq.empty null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-forall2 always returns true when both empty`` variant =
        TaskSeq.forall2 (fun _ _ -> false) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-forall2Async always returns true when both empty`` variant =
        TaskSeq.forall2Async (fun _ _ -> Task.fromResult false) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-forall2 always returns true when first is empty`` variant =
        TaskSeq.forall2 (fun _ _ -> false) (Gen.getEmptyVariant variant) (TaskSeq.ofList [ 1; 2; 3 ])
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-forall2 always returns true when second is empty`` variant =
        TaskSeq.forall2 (fun _ _ -> false) (TaskSeq.ofList [ 1; 2; 3 ]) (Gen.getEmptyVariant variant)
        |> Task.map (should be True)


module Forall2Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-forall2 sad path returns false when some pair fails`` variant =
        // source1 = source2 = 1..10; predicate: all x = y but x must also be < 5
        TaskSeq.forall2 (fun x y -> x = y && x < 5) (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-forall2Async sad path returns false when some pair fails`` variant =
        TaskSeq.forall2Async (fun x y -> task { return x = y && x < 5 }) (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-forall2 happy path returns true when all pairs satisfy predicate`` variant =
        // source1 = source2 = 1..10; predicate: x = y always true
        TaskSeq.forall2 (=) (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-forall2Async happy path returns true when all pairs satisfy predicate`` variant =
        TaskSeq.forall2Async (fun x y -> task { return x = y }) (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)
        |> Task.map (should be True)

    [<Fact>]
    let ``TaskSeq-forall2 stops at shorter sequence - longer does not affect result`` () =
        // source1 = [1;2;3], source2 = [1;2;3;0;0]
        // Without truncation, pairs (4,0) would fail the (=) predicate
        // With truncation at length 3, only (1,1) (2,2) (3,3) are checked → all pass
        TaskSeq.forall2 (=) (TaskSeq.ofList [ 1; 2; 3 ]) (TaskSeq.ofList [ 1; 2; 3; 0; 0 ])
        |> Task.map (should be True)

    [<Fact>]
    let ``TaskSeq-forall2 stops at shorter sequence - second shorter`` () =
        TaskSeq.forall2 (=) (TaskSeq.ofList [ 1; 2; 3; 0; 0 ]) (TaskSeq.ofList [ 1; 2; 3 ])
        |> Task.map (should be True)

    [<Fact>]
    let ``TaskSeq-forall2 works with different element types`` () =
        TaskSeq.forall2 (fun (x: int) (y: string) -> string x = y) (TaskSeq.ofList [ 1; 2; 3 ]) (TaskSeq.ofList [ "1"; "2"; "3" ])
        |> Task.map (should be True)

    [<Fact>]
    let ``TaskSeq-forall2Async works with different element types`` () =
        TaskSeq.forall2Async
            (fun (x: int) (y: string) -> task { return string x = y })
            (TaskSeq.ofList [ 1; 2; 3 ])
            (TaskSeq.ofList [ "1"; "2"; "3" ])
        |> Task.map (should be True)


module Forall2SideEffects =
    [<Fact>]
    let ``TaskSeq-forall2 _specialcase_ stops evaluating after first false pair`` () = task {
        let mutable i = 0
        let mutable j = 0

        let ts1 = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let ts2 = taskSeq {
            for _ in 0..9 do
                j <- j + 1
                yield j * 2 // offsets from ts1 after first item
        }

        // pair (1,2) fails: 1 = 2 is false
        let! result = TaskSeq.forall2 (=) ts1 ts2
        result |> should be False
        i |> should equal 1 // stopped at first pair
        j |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-forall2Async _specialcase_ stops evaluating after first false pair`` () = task {
        let mutable i = 0
        let mutable j = 0

        let ts1 = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let ts2 = taskSeq {
            for _ in 0..9 do
                j <- j + 1
                yield j * 2
        }

        let! result = TaskSeq.forall2Async (fun x y -> task { return x = y }) ts1 ts2
        result |> should be False
        i |> should equal 1
        j |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-forall2 mutated state can change result across iterations`` () = task {
        let mutable i = 0

        let ts1 = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        // Compare ts1 with a fixed sequence that always yields 1..10
        let staticSeq = TaskSeq.ofList [ 1..10 ]

        // first iteration: ts1 = 1..10, fixed = 1..10 → equal pairs → true
        let! result = TaskSeq.forall2 (fun x y -> x = y) ts1 staticSeq
        result |> should be True
        i |> should equal 10

        // second iteration: ts1 = 11..20 (side effects advance), fixed = 1..10 (fresh) → not equal → false
        let! result = TaskSeq.forall2 (fun x y -> x = y) ts1 staticSeq
        result |> should be False
        i |> should equal 11 // stopped at first mismatch
    }

    [<Fact>]
    let ``TaskSeq-forall2Async mutated state can change result across iterations`` () = task {
        let mutable i = 0

        let ts1 = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let staticSeq = TaskSeq.ofList [ 1..10 ]

        let! result = TaskSeq.forall2Async (fun x y -> task { return x = y }) ts1 staticSeq

        result |> should be True

        let! result = TaskSeq.forall2Async (fun x y -> task { return x = y }) ts1 staticSeq

        result |> should be False
        i |> should equal 11
    }


///////////
// fold2
///////////

module Fold2EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.fold2 (fun _ _ _ -> 0) 0 null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.fold2 (fun _ _ _ -> 0) 0 TaskSeq.empty null

        assertNullArg
        <| fun () -> TaskSeq.fold2Async (fun _ _ _ -> Task.fromResult 0) 0 null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.fold2Async (fun _ _ _ -> Task.fromResult 0) 0 TaskSeq.empty null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-fold2 returns initial state when both empty`` variant = task {
        let! result = TaskSeq.fold2 (fun acc _ _ -> acc + 1) 42 (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)

        result |> should equal 42
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-fold2Async returns initial state when both empty`` variant = task {
        let! result =
            TaskSeq.fold2Async (fun acc _ _ -> task { return acc + 1 }) 42 (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)

        result |> should equal 42
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-fold2 returns initial state when first is empty`` variant = task {
        let! result = TaskSeq.fold2 (fun acc _ _ -> acc + 1) 99 (Gen.getEmptyVariant variant) (TaskSeq.ofList [ 1; 2; 3 ])

        result |> should equal 99
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-fold2 returns initial state when second is empty`` variant = task {
        let! result = TaskSeq.fold2 (fun acc _ _ -> acc + 1) 99 (TaskSeq.ofList [ 1; 2; 3 ]) (Gen.getEmptyVariant variant)

        result |> should equal 99
    }


module Fold2Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-fold2 folds over all pairs`` variant = task {
        // source1 = source2 = 1..10; sum of products: 1*1 + 2*2 + ... + 10*10 = 385
        let! result = TaskSeq.fold2 (fun acc x y -> acc + x * y) 0 (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)

        result |> should equal 385
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-fold2Async folds over all pairs`` variant = task {
        let! result =
            TaskSeq.fold2Async (fun acc x y -> task { return acc + x * y }) 0 (Gen.getSeqImmutable variant) (Gen.getSeqImmutable variant)

        result |> should equal 385
    }

    [<Fact>]
    let ``TaskSeq-fold2 builds string from paired elements`` () = task {
        let! result =
            TaskSeq.fold2
                (fun (acc: StringBuilder) (x: int) (y: string) -> acc.Append(string x).Append(y))
                (StringBuilder())
                (TaskSeq.ofList [ 1; 2; 3 ])
                (TaskSeq.ofList [ "a"; "b"; "c" ])

        result.ToString() |> should equal "1a2b3c"
    }

    [<Fact>]
    let ``TaskSeq-fold2Async builds string from paired elements`` () = task {
        let! result =
            TaskSeq.fold2Async
                (fun (acc: StringBuilder) (x: int) (y: string) -> task { return acc.Append(string x).Append(y) })
                (StringBuilder())
                (TaskSeq.ofList [ 1; 2; 3 ])
                (TaskSeq.ofList [ "a"; "b"; "c" ])

        result.ToString() |> should equal "1a2b3c"
    }

    [<Fact>]
    let ``TaskSeq-fold2 stops at shorter - first shorter`` () = task {
        // source1 = [1;2;3], source2 = [10;20;30;40;50]
        // sum of products: 1*10 + 2*20 + 3*30 = 10+40+90 = 140 (not 4*40 + 5*50)
        let! result = TaskSeq.fold2 (fun acc x y -> acc + x * y) 0 (TaskSeq.ofList [ 1; 2; 3 ]) (TaskSeq.ofList [ 10; 20; 30; 40; 50 ])

        result |> should equal 140
    }

    [<Fact>]
    let ``TaskSeq-fold2 stops at shorter - second shorter`` () = task {
        let! result = TaskSeq.fold2 (fun acc x y -> acc + x * y) 0 (TaskSeq.ofList [ 10; 20; 30; 40; 50 ]) (TaskSeq.ofList [ 1; 2; 3 ])

        result |> should equal 140
    }

    [<Fact>]
    let ``TaskSeq-fold2 with equal-length sequences folds all pairs`` () = task {
        // Zips and counts pairs
        let! result =
            TaskSeq.fold2 (fun acc _ _ -> acc + 1) 0 (TaskSeq.ofList [ 1; 2; 3; 4; 5 ]) (TaskSeq.ofList [ 'a'; 'b'; 'c'; 'd'; 'e' ])

        result |> should equal 5
    }

    [<Fact>]
    let ``TaskSeq-fold2 with singleton sequences`` () = task {
        let! result = TaskSeq.fold2 (fun acc x y -> acc + x + y) 0 (TaskSeq.singleton 10) (TaskSeq.singleton 32)

        result |> should equal 42
    }


module Fold2SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-fold2 second fold has fresh state from side-effect sequences`` variant = task {
        let ts1 = Gen.getSeqWithSideEffect variant
        let ts2 = Gen.getSeqWithSideEffect variant

        // first iteration: ts1 = 1..10, ts2 = 1..10
        let! first = TaskSeq.fold2 (fun acc x y -> acc + x + y) 0 ts1 ts2
        first |> should equal 110 // sum of 2*(1+2+...+10) = 110

        // second iteration: ts1 = 11..20, ts2 = 11..20 (independent counters both advance)
        // sum of pairs: (11+11) + (12+12) + ... + (20+20) = 2*(11+...+20) = 2*155 = 310
        let! second = TaskSeq.fold2 (fun acc x y -> acc + x + y) 0 ts1 ts2
        second |> should equal 310
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-fold2Async second fold has fresh state from side-effect sequences`` variant = task {
        let ts1 = Gen.getSeqWithSideEffect variant
        let ts2 = Gen.getSeqWithSideEffect variant

        let! first = TaskSeq.fold2Async (fun acc x y -> task { return acc + x + y }) 0 ts1 ts2

        first |> should equal 110

        let! second = TaskSeq.fold2Async (fun acc x y -> task { return acc + x + y }) 0 ts1 ts2

        second |> should equal 310
    }
