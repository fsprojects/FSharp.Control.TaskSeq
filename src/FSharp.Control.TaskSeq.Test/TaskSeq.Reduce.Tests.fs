module TaskSeq.Tests.Reduce

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.reduce
// TaskSeq.reduceAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.reduce (fun a _ -> a) null

        assertNullArg
        <| fun () -> TaskSeq.reduceAsync (fun a _ -> Task.fromResult a) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-reduce raises on empty`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.reduce (fun a b -> a + b)
            |> Task.ignore

        |> should throwAsyncExact typeof<System.ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-reduceAsync raises on empty`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.reduceAsync (fun a b -> task { return a + b })
            |> Task.ignore

        |> should throwAsyncExact typeof<System.ArgumentException>

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-reduce folds from first element`` variant = task {
        // items are 1..10; sum = 55
        let! sum =
            Gen.getSeqImmutable variant
            |> TaskSeq.reduce (fun acc item -> acc + item)

        sum |> should equal 55
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-reduceAsync folds from first element`` variant = task {
        let! sum =
            Gen.getSeqImmutable variant
            |> TaskSeq.reduceAsync (fun acc item -> task { return acc + item })

        sum |> should equal 55
    }

    [<Fact>]
    let ``TaskSeq-reduce returns single element without calling folder`` () = task {
        let mutable called = false

        let! result =
            TaskSeq.singleton 42
            |> TaskSeq.reduce (fun _ _ ->
                called <- true
                failwith "should not be called")

        result |> should equal 42
        called |> should equal false
    }

    [<Fact>]
    let ``TaskSeq-reduceAsync returns single element without calling folder`` () = task {
        let mutable called = false

        let! result =
            TaskSeq.singleton 42
            |> TaskSeq.reduceAsync (fun _ _ -> task {
                called <- true
                return failwith "should not be called"
            })

        result |> should equal 42
        called |> should equal false
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-reduce uses first element as initial accumulator`` variant = task {
        // reduce must use element[0] as initial state; for 1..10 summing gives 55
        // if it used 0 as initial, sum would also be 55 — but we verify the folder is called n-1 times
        let mutable callCount = 0

        let! sum =
            Gen.getSeqImmutable variant
            |> TaskSeq.reduce (fun acc item ->
                callCount <- callCount + 1
                acc + item)

        sum |> should equal 55
        callCount |> should equal 9 // 10 elements => 9 reduce calls
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-reduce can concatenate strings`` variant = task {
        // items 1..10 as chars: ABCDEFGHIJ
        let! letters =
            Gen.getSeqImmutable variant
            |> TaskSeq.map (fun i -> string (char (i + 64)))
            |> TaskSeq.reduce (fun acc item -> acc + item)

        letters |> should equal "ABCDEFGHIJ"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-reduceAsync can concatenate strings`` variant = task {
        let! letters =
            Gen.getSeqImmutable variant
            |> TaskSeq.map (fun i -> string (char (i + 64)))
            |> TaskSeq.reduceAsync (fun acc item -> task { return acc + item })

        letters |> should equal "ABCDEFGHIJ"
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-reduce folds correctly with side-effecting sequences`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! sum = ts |> TaskSeq.reduce (fun acc item -> acc + item)

        sum |> should equal 55

        // second enumeration produces next 10 elements: 11..20, sum = 155
        let! sum2 = ts |> TaskSeq.reduce (fun acc item -> acc + item)

        sum2 |> should equal 155
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-reduceAsync folds correctly with side-effecting sequences`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! sum =
            ts
            |> TaskSeq.reduceAsync (fun acc item -> task { return acc + item })

        sum |> should equal 55

        let! sum2 =
            ts
            |> TaskSeq.reduceAsync (fun acc item -> task { return acc + item })

        sum2 |> should equal 155
    }
