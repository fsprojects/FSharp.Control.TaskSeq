module TaskSeq.Tests.DistinctUntilChanged

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.distinctUntilChanged / distinctUntilChangedWith / distinctUntilChangedWithAsync
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

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWith with null source raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.distinctUntilChangedWith (fun _ _ -> false) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-distinctUntilChangedWith has no effect on empty`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.distinctUntilChangedWith (fun _ _ -> false)
            |> TaskSeq.toListAsync
            |> Task.map (List.isEmpty >> should be True)
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWithAsync with null source raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.distinctUntilChangedWithAsync (fun _ _ -> task { return false }) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-distinctUntilChangedWithAsync has no effect on empty`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.distinctUntilChangedWithAsync (fun _ _ -> task { return false })
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

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWith with structural equality comparer behaves like distinctUntilChanged`` () = task {
        let ts =
            [ 'A'; 'A'; 'B'; 'Z'; 'C'; 'C'; 'Z'; 'C'; 'D'; 'D'; 'D'; 'Z' ]
            |> TaskSeq.ofList

        let! xs =
            ts
            |> TaskSeq.distinctUntilChangedWith (=)
            |> TaskSeq.toListAsync

        xs
        |> List.map string
        |> String.concat ""
        |> should equal "ABZCZCDZ"
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWith with always-true comparer returns only first element`` () = task {
        let! xs =
            taskSeq { yield! [ 1; 2; 3; 4; 5 ] }
            |> TaskSeq.distinctUntilChangedWith (fun _ _ -> true)
            |> TaskSeq.toListAsync

        xs |> should equal [ 1 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWith with always-false comparer returns all elements`` () = task {
        let! xs =
            taskSeq { yield! [ 1; 1; 2; 2; 3 ] }
            |> TaskSeq.distinctUntilChangedWith (fun _ _ -> false)
            |> TaskSeq.toListAsync

        xs |> should equal [ 1; 1; 2; 2; 3 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWith can use custom projection for equality`` () = task {
        // Treat values as equal if their absolute difference is <= 1
        let closeEnough a b = abs (a - b) <= 1

        let! xs =
            taskSeq { yield! [ 10; 11; 9; 20; 21; 5 ] }
            |> TaskSeq.distinctUntilChangedWith closeEnough
            |> TaskSeq.toListAsync

        // 10≈11 skip; 11≈9 skip (|11-9|=2? no, |11-9|=2>1, so keep 9); 9 vs 20 keep; 20≈21 skip; 21 vs 5 keep
        // Wait: |10-11|=1 skip 11; |10-9|=1 skip 9; 10 vs 20 keep 20; |20-21|=1 skip 21; 20 vs 5 keep 5
        xs |> should equal [ 10; 20; 5 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWith with single element returns singleton`` () = task {
        let! xs =
            taskSeq { yield 99 }
            |> TaskSeq.distinctUntilChangedWith (fun _ _ -> true)
            |> TaskSeq.toListAsync

        xs |> should equal [ 99 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWith case-insensitive string comparison`` () = task {
        let! xs =
            taskSeq { yield! [ "Hello"; "hello"; "HELLO"; "World"; "world" ] }
            |> TaskSeq.distinctUntilChangedWith (fun a b -> System.String.Compare(a, b, System.StringComparison.OrdinalIgnoreCase) = 0)
            |> TaskSeq.toListAsync

        xs |> should equal [ "Hello"; "World" ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWithAsync with structural equality behaves like distinctUntilChanged`` () = task {
        let ts = [ 1; 1; 2; 3; 3; 4 ] |> TaskSeq.ofList

        let! xs =
            ts
            |> TaskSeq.distinctUntilChangedWithAsync (fun a b -> task { return a = b })
            |> TaskSeq.toListAsync

        xs |> should equal [ 1; 2; 3; 4 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWithAsync with always-true async comparer returns only first element`` () = task {
        let! xs =
            taskSeq { yield! [ 10; 20; 30 ] }
            |> TaskSeq.distinctUntilChangedWithAsync (fun _ _ -> task { return true })
            |> TaskSeq.toListAsync

        xs |> should equal [ 10 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWithAsync with always-false async comparer returns all elements`` () = task {
        let! xs =
            taskSeq { yield! [ 5; 5; 5 ] }
            |> TaskSeq.distinctUntilChangedWithAsync (fun _ _ -> task { return false })
            |> TaskSeq.toListAsync

        xs |> should equal [ 5; 5; 5 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWithAsync can perform async work in comparer`` () = task {
        let mutable comparerCallCount = 0

        let asyncComparer a b = task {
            comparerCallCount <- comparerCallCount + 1
            return a = b
        }

        let! xs =
            taskSeq { yield! [ 1; 1; 2; 2; 3 ] }
            |> TaskSeq.distinctUntilChangedWithAsync asyncComparer
            |> TaskSeq.toListAsync

        xs |> should equal [ 1; 2; 3 ]
        // comparer called for each pair of consecutive elements (4 pairs for 5 elements)
        comparerCallCount |> should equal 4
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

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWith consumes every element exactly once`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..5 do
                count <- count + 1
                yield i
        }

        let! xs =
            ts
            |> TaskSeq.distinctUntilChangedWith (fun a b -> a = b)
            |> TaskSeq.toListAsync

        count |> should equal 5
        xs |> should equal [ 1; 2; 3; 4; 5 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctUntilChangedWithAsync consumes every element exactly once`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..5 do
                count <- count + 1
                yield i
        }

        let! xs =
            ts
            |> TaskSeq.distinctUntilChangedWithAsync (fun a b -> task { return a = b })
            |> TaskSeq.toListAsync

        count |> should equal 5
        xs |> should equal [ 1; 2; 3; 4; 5 ]
    }
