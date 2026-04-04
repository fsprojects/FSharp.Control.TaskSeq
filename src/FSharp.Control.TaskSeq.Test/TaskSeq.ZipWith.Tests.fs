module TaskSeq.Tests.ZipWith

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.zipWith
// TaskSeq.zipWithAsync
// TaskSeq.zipWith3
// TaskSeq.zipWithAsync3
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid for zipWith`` () =
        assertNullArg
        <| fun () -> TaskSeq.zipWith (+) null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.zipWith (+) TaskSeq.empty null

        assertNullArg
        <| fun () -> TaskSeq.zipWith (+) null (null: TaskSeq<int>)

    [<Fact>]
    let ``Null source is invalid for zipWithAsync`` () =
        assertNullArg
        <| fun () -> TaskSeq.zipWithAsync (fun a b -> Task.fromResult (a + b)) null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.zipWithAsync (fun a b -> Task.fromResult (a + b)) TaskSeq.empty null

    [<Fact>]
    let ``Null source is invalid for zipWith3`` () =
        assertNullArg
        <| fun () -> TaskSeq.zipWith3 (fun a b c -> a) null TaskSeq.empty TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.zipWith3 (fun a b c -> a) TaskSeq.empty null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.zipWith3 (fun a b c -> a) TaskSeq.empty TaskSeq.empty null

    [<Fact>]
    let ``Null source is invalid for zipWithAsync3`` () =
        let f a b c = Task.fromResult (a + b + c)

        assertNullArg
        <| fun () -> TaskSeq.zipWithAsync3 f null TaskSeq.empty TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.zipWithAsync3 f TaskSeq.empty null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.zipWithAsync3 f TaskSeq.empty TaskSeq.empty null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zipWith with two empty gives empty`` variant =
        TaskSeq.zipWith (+) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zipWith with one empty gives empty`` variant =
        TaskSeq.zipWith (+) TaskSeq.empty<int> (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zipWith3 with empties gives empty`` variant =
        TaskSeq.zipWith3 (fun a b c -> a + b + c) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> verifyEmpty


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-zipWith applies mapping correctly`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant
        let! result = TaskSeq.zipWith (+) one two |> TaskSeq.toArrayAsync

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) + (i + 1)))
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-zipWithAsync applies async mapping correctly`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant

        let! result =
            TaskSeq.zipWithAsync (fun a b -> Task.fromResult (a * b)) one two
            |> TaskSeq.toArrayAsync

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) * (i + 1)))
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-zipWith3 applies three-way mapping`` variant = task {
        let s1 = Gen.getSeqImmutable variant
        let s2 = Gen.getSeqImmutable variant
        let s3 = Gen.getSeqImmutable variant

        let! result =
            TaskSeq.zipWith3 (fun a b c -> a + b + c) s1 s2 s3
            |> TaskSeq.toArrayAsync

        result
        |> should equal (Array.init 10 (fun i -> 3 * (i + 1)))
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-zipWithAsync3 applies async three-way mapping`` variant = task {
        let s1 = Gen.getSeqImmutable variant
        let s2 = Gen.getSeqImmutable variant
        let s3 = Gen.getSeqImmutable variant

        let! result =
            TaskSeq.zipWithAsync3 (fun a b c -> Task.fromResult (a + b + c)) s1 s2 s3
            |> TaskSeq.toArrayAsync

        result
        |> should equal (Array.init 10 (fun i -> 3 * (i + 1)))
    }

    [<Fact>]
    let ``TaskSeq-zipWith truncates to shorter sequence`` () = task {
        let short = taskSeq {
            yield 1
            yield 2
        }

        let long = taskSeq { yield! [ 10..20 ] }
        let! result = TaskSeq.zipWith (+) short long |> TaskSeq.toArrayAsync
        result |> should equal [| 11; 13 |]
    }

    [<Fact>]
    let ``TaskSeq-zipWith string concatenation`` () = task {
        let keys = taskSeq {
            yield "a"
            yield "b"
            yield "c"
        }

        let values = taskSeq {
            yield 1
            yield 2
            yield 3
        }

        let! result =
            TaskSeq.zipWith (fun k v -> sprintf "%s=%d" k v) keys values
            |> TaskSeq.toArrayAsync

        result |> should equal [| "a=1"; "b=2"; "c=3" |]
    }

    [<Fact>]
    let ``TaskSeq-zipWith is equivalent to zip-then-map`` () = task {
        let s1 = taskSeq { yield! [ 1..5 ] }
        let s2 = taskSeq { yield! [ 10..14 ] }
        let! viaZipWith = TaskSeq.zipWith (+) s1 s2 |> TaskSeq.toArrayAsync
        let s1b = taskSeq { yield! [ 1..5 ] }
        let s2b = taskSeq { yield! [ 10..14 ] }

        let! viaZipMap =
            TaskSeq.zip s1b s2b
            |> TaskSeq.map (fun (a, b) -> a + b)
            |> TaskSeq.toArrayAsync

        viaZipWith |> should equal viaZipMap
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-zipWith on two side-effect seqs combines elements correctly`` variant = task {
        let s1 = Gen.getSeqWithSideEffect variant
        let s2 = Gen.getSeqWithSideEffect variant

        // Both sequences yield 1..10 on first iteration; side effects increment independently
        let! result = TaskSeq.zipWith (+) s1 s2 |> TaskSeq.toArrayAsync

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) + (i + 1)))
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-zipWithAsync on two side-effect seqs combines elements correctly`` variant = task {
        let s1 = Gen.getSeqWithSideEffect variant
        let s2 = Gen.getSeqWithSideEffect variant

        let! result =
            TaskSeq.zipWithAsync (fun a b -> Task.fromResult (a * b)) s1 s2
            |> TaskSeq.toArrayAsync

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) * (i + 1)))
    }

    [<Fact>]
    let ``TaskSeq-zipWith consumes both sequences one element at a time`` () = task {
        let mutable count1 = 0
        let mutable count2 = 0

        let s1 = taskSeq {
            for i in 1..5 do
                count1 <- count1 + 1
                yield i
        }

        let s2 = taskSeq {
            for i in 10..14 do
                count2 <- count2 + 1
                yield i
        }

        let! result = TaskSeq.zipWith (+) s1 s2 |> TaskSeq.toArrayAsync
        result |> should equal [| 11; 13; 15; 17; 19 |]
        count1 |> should equal 5
        count2 |> should equal 5
    }

    [<Fact>]
    let ``TaskSeq-zipWith truncates at shorter side-effect seq, output is correct`` () = task {
        let mutable longCount = 0

        let short = taskSeq { yield! [ 1; 2 ] }

        let long = taskSeq {
            for i in 10..20 do
                longCount <- longCount + 1
                yield i
        }

        let! result = TaskSeq.zipWith (+) short long |> TaskSeq.toArrayAsync
        result |> should equal [| 11; 13 |]
        // The implementation reads one element from each sequence to check for stop condition,
        // so the longer sequence is advanced one step beyond the last paired element.
        longCount |> should be (greaterThanOrEqualTo 2)
        longCount |> should be (lessThanOrEqualTo 3)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-zipWith3 on three side-effect seqs combines elements correctly`` variant = task {
        let s1 = Gen.getSeqWithSideEffect variant
        let s2 = Gen.getSeqWithSideEffect variant
        let s3 = Gen.getSeqWithSideEffect variant

        let! result =
            TaskSeq.zipWith3 (fun a b c -> a + b + c) s1 s2 s3
            |> TaskSeq.toArrayAsync

        result
        |> should equal (Array.init 10 (fun i -> 3 * (i + 1)))
    }
