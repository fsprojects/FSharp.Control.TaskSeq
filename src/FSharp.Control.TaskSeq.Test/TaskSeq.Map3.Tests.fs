module TaskSeq.Tests.Map3

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.map3
// TaskSeq.map3Async
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid for map3`` () =
        assertNullArg
        <| fun () -> TaskSeq.map3 (fun a b c -> a + b + c) null TaskSeq.empty TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.map3 (fun a b c -> a + b + c) TaskSeq.empty null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.map3 (fun a b c -> a + b + c) TaskSeq.empty TaskSeq.empty null

        assertNullArg
        <| fun () -> TaskSeq.map3 (fun a b c -> a + b + c) null null null

    [<Fact>]
    let ``Null source is invalid for map3Async`` () =
        assertNullArg
        <| fun () -> TaskSeq.map3Async (fun a b c -> task { return a + b + c }) null TaskSeq.empty TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.map3Async (fun a b c -> task { return a + b + c }) TaskSeq.empty null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.map3Async (fun a b c -> task { return a + b + c }) TaskSeq.empty TaskSeq.empty null

        assertNullArg
        <| fun () -> TaskSeq.map3Async (fun a b c -> task { return a + b + c }) null null null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map3 with all empty sequences returns empty`` variant =
        TaskSeq.map3 (fun a b c -> a + b + c) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant) (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map3Async with all empty sequences returns empty`` variant =
        TaskSeq.map3Async
            (fun a b c -> task { return a + b + c })
            (Gen.getEmptyVariant variant)
            (Gen.getEmptyVariant variant)
            (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map3 stops when first sequence is empty`` variant =
        TaskSeq.map3 (fun a b c -> a + b + c) (Gen.getEmptyVariant variant) (taskSeq { yield 1 }) (taskSeq { yield 2 })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map3 stops when second sequence is empty`` variant =
        TaskSeq.map3 (fun a b c -> a + b + c) (taskSeq { yield 1 }) (Gen.getEmptyVariant variant) (taskSeq { yield 2 })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map3 stops when third sequence is empty`` variant =
        TaskSeq.map3 (fun a b c -> a + b + c) (taskSeq { yield 1 }) (taskSeq { yield 2 }) (Gen.getEmptyVariant variant)
        |> verifyEmpty


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-map3 maps in correct order`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant
        let three = Gen.getSeqImmutable variant

        let! result =
            TaskSeq.map3 (fun a b c -> a + b + c) one two three
            |> TaskSeq.toArrayAsync

        result |> should haveLength 10

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) * 3))
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-map3Async maps in correct order`` variant = task {
        let one = Gen.getSeqImmutable variant
        let two = Gen.getSeqImmutable variant
        let three = Gen.getSeqImmutable variant

        let! result =
            TaskSeq.map3Async (fun a b c -> task { return a + b + c }) one two three
            |> TaskSeq.toArrayAsync

        result |> should haveLength 10

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) * 3))
    }

    [<Fact>]
    let ``TaskSeq-map3 applies mapping to corresponding elements`` () = task {
        let one = taskSeq { yield! [ 1..5 ] }
        let two = taskSeq { yield! [ 10..10..50 ] }
        let three = taskSeq { yield! [ 100..100..500 ] }

        let! result =
            TaskSeq.map3 (fun a b c -> a + b + c) one two three
            |> TaskSeq.toArrayAsync

        result |> should equal [| 111; 222; 333; 444; 555 |]
    }

    [<Fact>]
    let ``TaskSeq-map3 works with mixed types`` () = task {
        let one = taskSeq {
            yield "hello"
            yield "world"
        }

        let two = taskSeq {
            yield 1
            yield 2
        }

        let three = taskSeq {
            yield true
            yield false
        }

        let! result =
            TaskSeq.map3 (fun (s: string) (n: int) (b: bool) -> sprintf "%s-%d-%b" s n b) one two three
            |> TaskSeq.toArrayAsync

        result |> should equal [| "hello-1-true"; "world-2-false" |]
    }

    [<Fact>]
    let ``TaskSeq-map3 truncates to shortest sequence`` () = task {
        let one = taskSeq { yield! [ 1..10 ] }
        let two = taskSeq { yield! [ 1..5 ] }
        let three = taskSeq { yield! [ 1..3 ] }

        let! result =
            TaskSeq.map3 (fun a b c -> a + b + c) one two three
            |> TaskSeq.toArrayAsync

        result |> should haveLength 3
        result |> should equal [| 3; 6; 9 |]
    }

    [<Fact>]
    let ``TaskSeq-map3Async truncates to shortest sequence`` () = task {
        let one = taskSeq { yield! [ 1..10 ] }
        let two = taskSeq { yield! [ 1..5 ] }
        let three = taskSeq { yield! [ 1..3 ] }

        let! result =
            TaskSeq.map3Async (fun a b c -> task { return a + b + c }) one two three
            |> TaskSeq.toArrayAsync

        result |> should haveLength 3
        result |> should equal [| 3; 6; 9 |]
    }

    [<Fact>]
    let ``TaskSeq-map3 works with singleton sequences`` () = task {
        let! result =
            TaskSeq.map3 (fun a b c -> a + b + c) (TaskSeq.singleton 1) (TaskSeq.singleton 2) (TaskSeq.singleton 3)
            |> TaskSeq.toArrayAsync

        result |> should equal [| 6 |]
    }

    [<Fact>]
    let ``TaskSeq-map3Async works with singleton sequences`` () = task {
        let! result =
            TaskSeq.map3Async (fun a b c -> task { return a + b + c }) (TaskSeq.singleton 1) (TaskSeq.singleton 2) (TaskSeq.singleton 3)
            |> TaskSeq.toArrayAsync

        result |> should equal [| 6 |]
    }

    [<Fact>]
    let ``TaskSeq-map3 mapping function receives correct argument positions`` () = task {
        let! result =
            TaskSeq.map3
                (fun (a: string) (b: int) (c: bool) -> (a, b, c))
                (taskSeq { yield "x" })
                (taskSeq { yield 42 })
                (taskSeq { yield true })
            |> TaskSeq.toArrayAsync

        result |> should equal [| ("x", 42, true) |]
    }


module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-map3 can deal with side effects in sequences`` variant = task {
        let one = Gen.getSeqWithSideEffect variant
        let two = Gen.getSeqWithSideEffect variant
        let three = Gen.getSeqWithSideEffect variant

        let! result =
            TaskSeq.map3 (fun a b c -> a + b + c) one two three
            |> TaskSeq.toArrayAsync

        result |> should haveLength 10

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) * 3))
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-map3Async can deal with side effects in sequences`` variant = task {
        let one = Gen.getSeqWithSideEffect variant
        let two = Gen.getSeqWithSideEffect variant
        let three = Gen.getSeqWithSideEffect variant

        let! result =
            TaskSeq.map3Async (fun a b c -> task { return a + b + c }) one two three
            |> TaskSeq.toArrayAsync

        result |> should haveLength 10

        result
        |> should equal (Array.init 10 (fun i -> (i + 1) * 3))
    }
