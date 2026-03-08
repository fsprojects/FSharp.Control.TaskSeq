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

module UnequalLength =
    [<Fact>]
    let ``TaskSeq-zip stops at shorter first sequence`` () = task {
        // documented: "when one sequence is exhausted any remaining elements in the other sequence are ignored"
        let short = taskSeq { yield! [ 1..5 ] }
        let long = taskSeq { yield! [ 1..10 ] }
        let! combined = TaskSeq.zip short long |> TaskSeq.toArrayAsync
        combined |> should be (haveLength 5)

        combined
        |> should equal (Array.init 5 (fun i -> i + 1, i + 1))
    }

    [<Fact>]
    let ``TaskSeq-zip stops at shorter second sequence`` () = task {
        // documented: "when one sequence is exhausted any remaining elements in the other sequence are ignored"
        let long = taskSeq { yield! [ 1..10 ] }
        let short = taskSeq { yield! [ 1..3 ] }
        let! combined = TaskSeq.zip long short |> TaskSeq.toArrayAsync
        combined |> should be (haveLength 3)

        combined
        |> should equal (Array.init 3 (fun i -> i + 1, i + 1))
    }

    [<Fact>]
    let ``TaskSeq-zip with first sequence empty returns empty`` () =
        // documented: remaining elements in the longer sequence are ignored
        let empty = taskSeq { yield! ([]: int list) }
        let nonEmpty = taskSeq { yield! [ 1..10 ] }
        TaskSeq.zip empty nonEmpty |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-zip with second sequence empty returns empty`` () =
        // documented: remaining elements in the longer sequence are ignored
        let nonEmpty = taskSeq { yield! [ 1..10 ] }
        let empty = taskSeq { yield! ([]: int list) }
        TaskSeq.zip nonEmpty empty |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-zip with singleton first and longer second returns singleton`` () = task {
        let one = taskSeq { yield 42 }
        let many = taskSeq { yield! [ 1..10 ] }
        let! combined = TaskSeq.zip one many |> TaskSeq.toArrayAsync
        combined |> should equal [| (42, 1) |]
    }

    [<Fact>]
    let ``TaskSeq-zip with longer first and singleton second returns singleton`` () = task {
        let many = taskSeq { yield! [ 1..10 ] }
        let one = taskSeq { yield 99 }
        let! combined = TaskSeq.zip many one |> TaskSeq.toArrayAsync
        combined |> should equal [| (1, 99) |]
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

//
// TaskSeq.zip3
//

module EmptySeqZip3 =
    [<Fact>]
    let ``Null source is invalid for zip3`` () =
        assertNullArg
        <| fun () -> TaskSeq.zip3 null TaskSeq.empty TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.zip3 TaskSeq.empty null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.zip3 TaskSeq.empty TaskSeq.empty null

        assertNullArg <| fun () -> TaskSeq.zip3 null null null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zip3 can zip empty sequences`` variant =
        TaskSeq.zip3 (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zip3 stops at first exhausted sequence`` variant =
        // second and third are non-empty, first is empty → result is empty
        TaskSeq.zip3 (Gen.getEmptyVariant variant) (taskSeq { yield 1 }) (taskSeq { yield 2 })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zip3 stops when second sequence is empty`` variant =
        TaskSeq.zip3 (taskSeq { yield 1 }) (Gen.getEmptyVariant variant) (taskSeq { yield 2 })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-zip3 stops when third sequence is empty`` variant =
        TaskSeq.zip3 (taskSeq { yield 1 }) (taskSeq { yield 2 }) (Gen.getEmptyVariant variant)
        |> verifyEmpty

module ImmutableZip3 =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-zip3 zips in correct order`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant
        let three = Gen.getSeqImmutable variant
        let! combined = TaskSeq.zip3 one two three |> TaskSeq.toArrayAsync

        combined |> should haveLength 10

        combined
        |> should equal (Array.init 10 (fun x -> x + 1, x + 1, x + 1))
    }

    [<Fact>]
    let ``TaskSeq-zip3 produces correct triples with mixed types`` () = task {
        let one = taskSeq {
            yield "a"
            yield "b"
        }

        let two = taskSeq {
            yield 1
            yield 2
        }

        let three = taskSeq {
            yield true
            yield false
        }

        let! combined = TaskSeq.zip3 one two three |> TaskSeq.toArrayAsync

        combined
        |> should equal [| ("a", 1, true); ("b", 2, false) |]
    }

    [<Fact>]
    let ``TaskSeq-zip3 truncates to shortest sequence`` () = task {
        let one = taskSeq { yield! [ 1..10 ] }
        let two = taskSeq { yield! [ 1..5 ] }
        let three = taskSeq { yield! [ 1..3 ] }
        let! combined = TaskSeq.zip3 one two three |> TaskSeq.toArrayAsync

        combined |> should haveLength 3

        combined
        |> should equal [| (1, 1, 1); (2, 2, 2); (3, 3, 3) |]
    }

    [<Fact>]
    let ``TaskSeq-zip3 works with a single-element sequences`` () = task {
        let! combined =
            TaskSeq.zip3 (TaskSeq.singleton 1) (TaskSeq.singleton "x") (TaskSeq.singleton true)
            |> TaskSeq.toArrayAsync

        combined |> should equal [| (1, "x", true) |]
    }

module SideEffectsZip3 =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-zip3 can deal with side effects in sequences`` variant = task {
        let one = Gen.getSeqWithSideEffect variant
        let two = Gen.getSeqWithSideEffect variant
        let three = Gen.getSeqWithSideEffect variant
        let! combined = TaskSeq.zip3 one two three |> TaskSeq.toArrayAsync

        combined
        |> Array.forall (fun (x, y, z) -> x = y && y = z)
        |> should be True

        combined |> should haveLength 10
    }
