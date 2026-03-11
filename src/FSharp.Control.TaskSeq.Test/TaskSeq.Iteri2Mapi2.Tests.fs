module TaskSeq.Tests.Iteri2Mapi2

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.iteri2
// TaskSeq.iteri2Async
// TaskSeq.mapi2
// TaskSeq.mapi2Async
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid for iteri2`` () =
        assertNullArg
        <| fun () -> TaskSeq.iteri2 (fun _ _ _ -> ()) null (TaskSeq.empty<int>)

        assertNullArg
        <| fun () -> TaskSeq.iteri2 (fun _ _ _ -> ()) (TaskSeq.empty<int>) null

    [<Fact>]
    let ``Null source is invalid for iteri2Async`` () =
        assertNullArg
        <| fun () -> TaskSeq.iteri2Async (fun _ _ _ -> Task.fromResult ()) null (TaskSeq.empty<int>)

        assertNullArg
        <| fun () -> TaskSeq.iteri2Async (fun _ _ _ -> Task.fromResult ()) (TaskSeq.empty<int>) null

    [<Fact>]
    let ``Null source is invalid for mapi2`` () =
        assertNullArg
        <| fun () -> TaskSeq.mapi2 (fun _ x _ -> x) null (TaskSeq.empty<int>)

        assertNullArg
        <| fun () -> TaskSeq.mapi2 (fun _ x _ -> x) (TaskSeq.empty<int>) null

    [<Fact>]
    let ``Null source is invalid for mapi2Async`` () =
        assertNullArg
        <| fun () -> TaskSeq.mapi2Async (fun _ x _ -> Task.fromResult x) null (TaskSeq.empty<int>)

        assertNullArg
        <| fun () -> TaskSeq.mapi2Async (fun _ x _ -> Task.fromResult x) (TaskSeq.empty<int>) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-iteri2 does nothing when first source is empty`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let mutable count = 0
        do! TaskSeq.iteri2 (fun _ _ _ -> count <- count + 1) tq (TaskSeq.ofSeq [ 1; 2; 3 ])
        count |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-iteri2 does nothing when second source is empty`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let mutable count = 0
        do! TaskSeq.iteri2 (fun _ _ _ -> count <- count + 1) (TaskSeq.ofSeq [ 1; 2; 3 ]) tq
        count |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-mapi2 returns empty when first source is empty`` variant =
        TaskSeq.mapi2 (fun i x y -> i + x + y) (Gen.getEmptyVariant variant) (TaskSeq.ofSeq [ 1..10 ])
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-mapi2 returns empty when second source is empty`` variant =
        TaskSeq.mapi2 (fun i x y -> i + x + y) (TaskSeq.ofSeq [ 1..10 ]) (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-mapi2Async returns empty when first source is empty`` variant =
        TaskSeq.mapi2Async (fun i x y -> task { return i + x + y }) (Gen.getEmptyVariant variant) (TaskSeq.ofSeq [ 1..10 ])
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-mapi2Async returns empty when second source is empty`` variant =
        TaskSeq.mapi2Async (fun i x y -> task { return i + x + y }) (TaskSeq.ofSeq [ 1..10 ]) (Gen.getEmptyVariant variant)
        |> verifyEmpty


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iteri2 visits all elements of equal-length sequences`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let mutable sum = 0
        do! TaskSeq.iteri2 (fun _ x y -> sum <- sum + x + y) tq (TaskSeq.ofSeq [ 1..10 ])
        sum |> should equal 110 // (1..10) + (1..10) = 55 + 55
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iteri2 passes correct zero-based indices`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let mutable indexSum = 0
        do! TaskSeq.iteri2 (fun i _ _ -> indexSum <- indexSum + i) tq (TaskSeq.ofSeq [ 1..10 ])
        indexSum |> should equal 45 // 0+1+2+...+9
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iteri2Async visits all elements of equal-length sequences`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let mutable sum = 0

        do! TaskSeq.iteri2Async (fun _ x y -> task { sum <- sum + x + y }) tq (TaskSeq.ofSeq [ 1..10 ])

        sum |> should equal 110
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iteri2Async passes correct zero-based indices`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let mutable indexSum = 0

        do! TaskSeq.iteri2Async (fun i _ _ -> task { indexSum <- indexSum + i }) tq (TaskSeq.ofSeq [ 1..10 ])

        indexSum |> should equal 45
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-mapi2 maps in correct order with correct indices`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let results = ResizeArray()

        do!
            TaskSeq.mapi2 (fun i x y -> (i, x, y)) tq (TaskSeq.ofSeq [ 10..19 ])
            |> TaskSeq.iter (fun t -> results.Add t)

        let indices, xs, ys = results |> Seq.toList |> List.unzip3

        indices |> should equal [ 0..9 ]
        xs |> should equal [ 1..10 ]
        ys |> should equal [ 10..19 ]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-mapi2Async maps in correct order with correct indices`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let results = ResizeArray()

        do!
            TaskSeq.mapi2Async (fun i x y -> task { return (i, x, y) }) tq (TaskSeq.ofSeq [ 10..19 ])
            |> TaskSeq.iter (fun t -> results.Add t)

        let indices, xs, ys = results |> Seq.toList |> List.unzip3

        indices |> should equal [ 0..9 ]
        xs |> should equal [ 1..10 ]
        ys |> should equal [ 10..19 ]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-mapi2 using heterogeneous element types`` variant = task {
        let tq = Gen.getSeqImmutable variant

        let result =
            TaskSeq.mapi2
                (fun i x (s: string) -> sprintf "%d:%d:%s" i x s)
                tq
                (TaskSeq.ofSeq [ "a"; "b"; "c"; "d"; "e"; "f"; "g"; "h"; "i"; "j" ])

        let! lst = result |> TaskSeq.toListAsync

        lst
        |> should equal [ "0:1:a"; "1:2:b"; "2:3:c"; "3:4:d"; "4:5:e"; "5:6:f"; "6:7:g"; "7:8:h"; "8:9:i"; "9:10:j" ]
    }


module Truncation =
    [<Fact>]
    let ``TaskSeq-iteri2 stops at the shorter first sequence`` () = task {
        let mutable count = 0
        do! TaskSeq.iteri2 (fun _ _ _ -> count <- count + 1) (TaskSeq.ofSeq [ 1..3 ]) (TaskSeq.ofSeq [ 1..10 ])
        count |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-iteri2 stops at the shorter second sequence`` () = task {
        let mutable count = 0
        do! TaskSeq.iteri2 (fun _ _ _ -> count <- count + 1) (TaskSeq.ofSeq [ 1..10 ]) (TaskSeq.ofSeq [ 1..4 ])
        count |> should equal 4
    }

    [<Fact>]
    let ``TaskSeq-iteri2Async stops at the shorter first sequence`` () = task {
        let mutable count = 0

        do! TaskSeq.iteri2Async (fun _ _ _ -> task { count <- count + 1 }) (TaskSeq.ofSeq [ 1..3 ]) (TaskSeq.ofSeq [ 1..10 ])

        count |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-iteri2Async stops at the shorter second sequence`` () = task {
        let mutable count = 0

        do! TaskSeq.iteri2Async (fun _ _ _ -> task { count <- count + 1 }) (TaskSeq.ofSeq [ 1..10 ]) (TaskSeq.ofSeq [ 1..4 ])

        count |> should equal 4
    }

    [<Fact>]
    let ``TaskSeq-mapi2 stops at the shorter first sequence`` () = task {
        let result = TaskSeq.mapi2 (fun i x y -> i + x + y) (TaskSeq.ofSeq [ 1..3 ]) (TaskSeq.ofSeq [ 1..10 ])

        let! lst = result |> TaskSeq.toListAsync
        lst |> should equal [ 2; 5; 8 ] // (0+1+1), (1+2+2), (2+3+3)
    }

    [<Fact>]
    let ``TaskSeq-mapi2 stops at the shorter second sequence`` () = task {
        let result = TaskSeq.mapi2 (fun i x y -> i + x + y) (TaskSeq.ofSeq [ 1..10 ]) (TaskSeq.ofSeq [ 1..4 ])

        let! lst = result |> TaskSeq.toListAsync
        lst |> should equal [ 2; 5; 8; 11 ] // (0+1+1), (1+2+2), (2+3+3), (3+4+4)
    }

    [<Fact>]
    let ``TaskSeq-mapi2Async stops at the shorter first sequence`` () = task {
        let result = TaskSeq.mapi2Async (fun i x y -> task { return i + x + y }) (TaskSeq.ofSeq [ 1..3 ]) (TaskSeq.ofSeq [ 1..10 ])

        let! lst = result |> TaskSeq.toListAsync
        lst |> should equal [ 2; 5; 8 ]
    }

    [<Fact>]
    let ``TaskSeq-mapi2Async stops at the shorter second sequence`` () = task {
        let result = TaskSeq.mapi2Async (fun i x y -> task { return i + x + y }) (TaskSeq.ofSeq [ 1..10 ]) (TaskSeq.ofSeq [ 1..4 ])

        let! lst = result |> TaskSeq.toListAsync
        lst |> should equal [ 2; 5; 8; 11 ]
    }

    [<Fact>]
    let ``TaskSeq-iteri2 index is always zero-based and matches iteration count even with truncation`` () = task {
        let indices = ResizeArray()
        do! TaskSeq.iteri2 (fun i _ _ -> indices.Add i) (TaskSeq.ofSeq [ 1..5 ]) (TaskSeq.ofSeq [ 10..12 ])
        indices |> Seq.toList |> should equal [ 0; 1; 2 ]
    }

    [<Fact>]
    let ``TaskSeq-mapi2 index is always zero-based and matches iteration count even with truncation`` () = task {
        let result = TaskSeq.mapi2 (fun i _ _ -> i) (TaskSeq.ofSeq [ 1..5 ]) (TaskSeq.ofSeq [ 10..12 ])

        let! lst = result |> TaskSeq.toListAsync
        lst |> should equal [ 0; 1; 2 ]
    }


module SideEffects =
    [<Fact>]
    let ``TaskSeq-mapi2 is lazy and does not evaluate until iterated`` () =
        let mutable sideEffect = 0

        let ts1 = taskSeq {
            sideEffect <- sideEffect + 1
            yield 1
            sideEffect <- sideEffect + 1
            yield 2
        }

        let ts2 = TaskSeq.ofSeq [ 10; 20 ]

        // building the mapped sequence should not evaluate anything
        let _ = TaskSeq.mapi2 (fun i x y -> (i, x, y)) ts1 ts2
        sideEffect |> should equal 0

    [<Fact>]
    let ``TaskSeq-iteri2 evaluates side effects in both sequences`` () = task {
        let mutable s1 = 0
        let mutable s2 = 0

        let ts1 = taskSeq {
            s1 <- s1 + 1
            yield 1
            s1 <- s1 + 1
            yield 2
        }

        let ts2 = taskSeq {
            s2 <- s2 + 1
            yield 10
            s2 <- s2 + 1
            yield 20
        }

        do! TaskSeq.iteri2 (fun _ _ _ -> ()) ts1 ts2
        s1 |> should equal 2
        s2 |> should equal 2
    }

    [<Fact>]
    let ``TaskSeq-iteri2Async evaluates side effects in both sequences`` () = task {
        let mutable s1 = 0
        let mutable s2 = 0

        let ts1 = taskSeq {
            s1 <- s1 + 1
            yield 1
            s1 <- s1 + 1
            yield 2
        }

        let ts2 = taskSeq {
            s2 <- s2 + 1
            yield 10
            s2 <- s2 + 1
            yield 20
        }

        do! TaskSeq.iteri2Async (fun _ _ _ -> task { () }) ts1 ts2
        s1 |> should equal 2
        s2 |> should equal 2
    }
