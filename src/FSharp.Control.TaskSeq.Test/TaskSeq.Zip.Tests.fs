module TaskSeq.Tests.Zip

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.zip
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg <| fun () -> TaskSeq.zip null TaskSeq.empty
        assertNullArg <| fun () -> TaskSeq.zip TaskSeq.empty null
        assertNullArg <| fun () -> TaskSeq.zip null null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zip can zip empty sequences v1`` variant =
        TaskSeq.zip (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zip can zip empty sequences v2`` variant =
        TaskSeq.zip TaskSeq.empty<int> (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zip can zip empty sequences v3`` variant =
        TaskSeq.zip (Gen.getEmptyVariant variant) TaskSeq.empty<int>
        |> verifyEmpty


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-zip zips in correct order`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant
        let combined = TaskSeq.zip one two
        let! combined = TaskSeq.toArrayAsync combined

        combined
        |> Array.forall (fun (x, y) -> x = y)
        |> should be True

        combined |> should be (haveLength 10)

        combined
        |> should equal (Array.init 10 (fun x -> x + 1, x + 1))
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-zip zips in correct order for differently delayed sequences`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = taskSeq { yield! [ 1..10 ] }
        let combined = TaskSeq.zip one two
        let! combined = TaskSeq.toArrayAsync combined

        combined
        |> Array.forall (fun (x, y) -> x = y)
        |> should be True

        combined |> should be (haveLength 10)

        combined
        |> should equal (Array.init 10 (fun x -> x + 1, x + 1))
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-zip zips can deal with side effects in sequences`` variant = task {
        let one = Gen.getSeqWithSideEffect variant
        let two = Gen.getSeqWithSideEffect variant
        let combined = TaskSeq.zip one two
        let! combined = TaskSeq.toArrayAsync combined

        combined
        |> Array.forall (fun (x, y) -> x = y)
        |> should be True

        combined |> should be (haveLength 10)

        combined
        |> should equal (Array.init 10 (fun x -> x + 1, x + 1))
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-zip zips combine a side-effect-free, and a side-effect-full sequence`` variant = task {
        let one = Gen.getSeqWithSideEffect variant
        let two = taskSeq { yield! [ 1..10 ] }
        let combined = TaskSeq.zip one two
        let! combined = TaskSeq.toArrayAsync combined

        combined
        |> Array.forall (fun (x, y) -> x = y)
        |> should be True

        combined |> should be (haveLength 10)

        combined
        |> should equal (Array.init 10 (fun x -> x + 1, x + 1))
    }

module Performance =
    [<Theory; InlineData 100; InlineData 1_000; InlineData 10_000; InlineData 100_000>]
    let ``TaskSeq-zip zips large sequences just fine`` length = task {
        let one = Gen.sideEffectTaskSeqMicro 10L<µs> 50L<µs> length
        let two = Gen.sideEffectTaskSeq_Sequential length
        let combined = TaskSeq.zip one two
        let! combined = TaskSeq.toArrayAsync combined

        combined
        |> Array.forall (fun (x, y) -> x = y)
        |> should be True

        combined |> should be (haveLength length)
        combined |> Array.last |> should equal (length, length)
    }

module Other =
    [<Fact>]
    let ``TaskSeq-zip zips different types`` () = task {
        let one = taskSeq {
            yield "one"
            yield "two"
        }

        let two = taskSeq {
            yield 42L
            yield 43L
        }

        let combined = TaskSeq.zip one two
        let! combined = TaskSeq.toArrayAsync combined

        combined |> should equal [| ("one", 42L); ("two", 43L) |]
    }
