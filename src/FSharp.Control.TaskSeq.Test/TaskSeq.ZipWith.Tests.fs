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
