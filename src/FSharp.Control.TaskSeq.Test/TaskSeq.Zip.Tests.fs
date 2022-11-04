module TaskSeq.Tests.Zip

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.zip
//

module EmptySeq =
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

    [<Theory; InlineData true; InlineData false>]
    let ``TaskSeq-zip throws on unequal lengths, variant`` leftThrows = task {
        let long = Gen.sideEffectTaskSeq 11
        let short = Gen.sideEffectTaskSeq 10

        let combined =
            if leftThrows then
                TaskSeq.zip short long
            else
                TaskSeq.zip long short

        fun () -> TaskSeq.toArrayAsync combined |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Theory; InlineData true; InlineData false>]
    let ``TaskSeq-zip throws on unequal lengths with empty seq`` leftThrows = task {
        let one = Gen.sideEffectTaskSeq 1

        let combined =
            if leftThrows then
                TaskSeq.zip TaskSeq.empty one
            else
                TaskSeq.zip one TaskSeq.empty

        fun () -> TaskSeq.toArrayAsync combined |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }
