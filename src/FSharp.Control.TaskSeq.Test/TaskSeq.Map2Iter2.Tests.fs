module TaskSeq.Tests.Map2Iter2

open System.Threading.Tasks
open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.iter2
// TaskSeq.iter2Async
// TaskSeq.map2
// TaskSeq.map2Async
//

module Iter2EmptySeq =
    [<Fact>]
    let ``Null source is invalid for iter2`` () =
        assertNullArg
        <| fun () -> TaskSeq.iter2 (fun _ _ -> ()) null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.iter2 (fun _ _ -> ()) TaskSeq.empty null

    [<Fact>]
    let ``Null source is invalid for iter2Async`` () =
        assertNullArg
        <| fun () -> TaskSeq.iter2Async (fun _ _ -> Task.fromResult ()) null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.iter2Async (fun _ _ -> Task.fromResult ()) TaskSeq.empty null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-iter2 does nothing on two empty sequences`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let mutable sum = 0
        do! TaskSeq.iter2 (fun a b -> sum <- sum + a + b) tq tq
        sum |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-iter2 does nothing when first sequence is empty`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let mutable sum = 0
        do! TaskSeq.iter2 (fun a b -> sum <- sum + a + b) tq (taskSeq { yield! [ 1..10 ] })
        sum |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-iter2 does nothing when second sequence is empty`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let mutable sum = 0
        do! TaskSeq.iter2 (fun a b -> sum <- sum + a + b) (taskSeq { yield! [ 1..10 ] }) tq
        sum |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-iter2Async does nothing on two empty sequences`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let mutable sum = 0

        do!
            TaskSeq.iter2Async
                (fun a b ->
                    sum <- sum + a + b
                    Task.fromResult ())
                tq
                tq

        sum |> should equal 0
    }

module Iter2Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iter2 visits all paired elements in order`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant
        let results = System.Collections.Generic.List<int * int>()
        do! TaskSeq.iter2 (fun a b -> results.Add(a, b)) one two
        results.Count |> should equal 10

        results
        |> Seq.forall (fun (a, b) -> a = b)
        |> should be True

        results
        |> Seq.map fst
        |> Seq.toArray
        |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iter2Async visits all paired elements in order`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant
        let results = System.Collections.Generic.List<int * int>()

        do!
            TaskSeq.iter2Async
                (fun a b ->
                    results.Add(a, b)
                    Task.fromResult ())
                one
                two

        results.Count |> should equal 10

        results
        |> Seq.forall (fun (a, b) -> a = b)
        |> should be True

        results
        |> Seq.map fst
        |> Seq.toArray
        |> should equal [| 1..10 |]
    }

    [<Fact>]
    let ``TaskSeq-iter2 truncates to shorter sequence when first is shorter`` () = task {
        let short = taskSeq { yield! [ 1..3 ] }
        let long = taskSeq { yield! [ 1..10 ] }
        let mutable count = 0
        do! TaskSeq.iter2 (fun _ _ -> count <- count + 1) short long
        count |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-iter2 truncates to shorter sequence when second is shorter`` () = task {
        let long = taskSeq { yield! [ 1..10 ] }
        let short = taskSeq { yield! [ 1..3 ] }
        let mutable count = 0
        do! TaskSeq.iter2 (fun _ _ -> count <- count + 1) long short
        count |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-iter2 can combine different element types`` () = task {
        let ints = taskSeq { yield! [ 1; 2; 3 ] }
        let strs = taskSeq { yield! [ "a"; "b"; "c" ] }
        let results = System.Collections.Generic.List<int * string>()
        do! TaskSeq.iter2 (fun n s -> results.Add(n, s)) ints strs
        results.Count |> should equal 3

        results
        |> Seq.toList
        |> should equal [ (1, "a"); (2, "b"); (3, "c") ]
    }

module Map2EmptySeq =
    [<Fact>]
    let ``Null source is invalid for map2`` () =
        assertNullArg
        <| fun () -> TaskSeq.map2 (fun _ _ -> ()) null TaskSeq.empty<int>

        assertNullArg
        <| fun () -> TaskSeq.map2 (fun _ _ -> ()) TaskSeq.empty<int> null

    [<Fact>]
    let ``Null source is invalid for map2Async`` () =
        assertNullArg
        <| fun () -> TaskSeq.map2Async (fun _ _ -> Task.fromResult ()) null TaskSeq.empty<int>

        assertNullArg
        <| fun () -> TaskSeq.map2Async (fun _ _ -> Task.fromResult ()) TaskSeq.empty<int> null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map2 returns empty on two empty sequences`` variant =
        TaskSeq.map2 (fun a b -> a + b) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map2 returns empty when first sequence is empty`` variant =
        TaskSeq.map2 (fun a b -> a + b) (Gen.getEmptyVariant variant) (taskSeq { yield! [ 1..10 ] })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map2 returns empty when second sequence is empty`` variant =
        TaskSeq.map2 (fun a b -> a + b) (taskSeq { yield! [ 1..10 ] }) (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map2Async returns empty on two empty sequences`` variant =
        TaskSeq.map2Async (fun a b -> Task.fromResult (a + b)) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> verifyEmpty

module Map2Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-map2 maps all paired elements in order`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant

        let! result =
            TaskSeq.map2 (fun a b -> a + b) one two
            |> TaskSeq.toArrayAsync

        result |> should haveLength 10

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) * 2))
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-map2Async maps all paired elements in order`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant

        let! result =
            TaskSeq.map2Async (fun a b -> Task.fromResult (a + b)) one two
            |> TaskSeq.toArrayAsync

        result |> should haveLength 10

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) * 2))
    }

    [<Fact>]
    let ``TaskSeq-map2 truncates to shorter sequence when first is shorter`` () = task {
        let short = taskSeq { yield! [ 1..3 ] }
        let long = taskSeq { yield! [ 10..19 ] }

        let! result =
            TaskSeq.map2 (fun a b -> a + b) short long
            |> TaskSeq.toArrayAsync

        result |> should haveLength 3
        result |> should equal [| 11; 13; 15 |]
    }

    [<Fact>]
    let ``TaskSeq-map2 truncates to shorter sequence when second is shorter`` () = task {
        let long = taskSeq { yield! [ 10..19 ] }
        let short = taskSeq { yield! [ 1..3 ] }

        let! result =
            TaskSeq.map2 (fun a b -> a + b) long short
            |> TaskSeq.toArrayAsync

        result |> should haveLength 3
        result |> should equal [| 11; 13; 15 |]
    }

    [<Fact>]
    let ``TaskSeq-map2 can produce different types`` () = task {
        let ints = taskSeq { yield! [ 1; 2; 3 ] }
        let strs = taskSeq { yield! [ "a"; "b"; "c" ] }

        let! result =
            TaskSeq.map2 (fun n s -> sprintf "%d%s" n s) ints strs
            |> TaskSeq.toArrayAsync

        result |> should equal [| "1a"; "2b"; "3c" |]
    }

    [<Fact>]
    let ``TaskSeq-map2 works with equal-length sequences`` () = task {
        let s1 = taskSeq { yield! [ 1..5 ] }
        let s2 = taskSeq { yield! [ 10..14 ] }

        let! result =
            TaskSeq.map2 (fun a b -> a * b) s1 s2
            |> TaskSeq.toArrayAsync

        result |> should haveLength 5
        result |> should equal [| 10; 22; 36; 52; 70 |]
    }

    [<Fact>]
    let ``TaskSeq-map2Async can use async work in mapping`` () = task {
        let s1 = taskSeq { yield! [ 1..3 ] }
        let s2 = taskSeq { yield! [ 4..6 ] }

        let! result =
            TaskSeq.map2Async
                (fun a b -> task {
                    do! Task.Delay(0)
                    return a + b
                })
                s1
                s2
            |> TaskSeq.toArrayAsync

        result |> should equal [| 5; 7; 9 |]
    }

module Map2SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-map2 works correctly with side-effect sequences`` variant = task {
        let one = Gen.getSeqWithSideEffect variant
        let two = Gen.getSeqWithSideEffect variant

        let! result =
            TaskSeq.map2 (fun a b -> a + b) one two
            |> TaskSeq.toArrayAsync

        result |> should haveLength 10

        result
        |> Array.forall (fun x -> x % 2 = 0)
        |> should be True
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iter2 works correctly with side-effect sequences`` variant = task {
        let one = Gen.getSeqWithSideEffect variant
        let two = Gen.getSeqWithSideEffect variant
        let mutable count = 0
        do! TaskSeq.iter2 (fun _ _ -> count <- count + 1) one two
        count |> should equal 10
    }
