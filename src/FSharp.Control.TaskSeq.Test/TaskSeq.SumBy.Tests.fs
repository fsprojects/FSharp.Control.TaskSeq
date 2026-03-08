module TaskSeq.Tests.SumBy

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.sum
// TaskSeq.sumBy
// TaskSeq.sumByAsync
// TaskSeq.average
// TaskSeq.averageBy
// TaskSeq.averageByAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid for sum`` () =
        assertNullArg
        <| fun () -> TaskSeq.sum (null: System.Collections.Generic.IAsyncEnumerable<int>)

    [<Fact>]
    let ``Null source is invalid for sumBy`` () =
        assertNullArg
        <| fun () -> TaskSeq.sumBy id (null: System.Collections.Generic.IAsyncEnumerable<int>)

    [<Fact>]
    let ``Null source is invalid for sumByAsync`` () =
        assertNullArg
        <| fun () -> TaskSeq.sumByAsync (id >> Task.fromResult) (null: System.Collections.Generic.IAsyncEnumerable<int>)

    [<Fact>]
    let ``Null source is invalid for average`` () =
        assertNullArg
        <| fun () -> TaskSeq.average (null: System.Collections.Generic.IAsyncEnumerable<float>)

    [<Fact>]
    let ``Null source is invalid for averageBy`` () =
        assertNullArg
        <| fun () -> TaskSeq.averageBy float (null: System.Collections.Generic.IAsyncEnumerable<int>)

    [<Fact>]
    let ``Null source is invalid for averageByAsync`` () =
        assertNullArg
        <| fun () -> TaskSeq.averageByAsync (float >> Task.fromResult) (null: System.Collections.Generic.IAsyncEnumerable<int>)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sum returns zero on empty`` variant = task {
        let! result = Gen.getEmptyVariant variant |> TaskSeq.sum
        result |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sumBy returns zero on empty`` variant = task {
        let! result = Gen.getEmptyVariant variant |> TaskSeq.sumBy id
        result |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sumByAsync returns zero on empty`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.sumByAsync Task.fromResult

        result |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-average raises on empty`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.map float
            |> TaskSeq.average
            |> Task.ignore

        |> should throwAsyncExact typeof<System.ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-averageBy raises on empty`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.averageBy float
            |> Task.ignore

        |> should throwAsyncExact typeof<System.ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-averageByAsync raises on empty`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.averageByAsync (float >> Task.fromResult)
            |> Task.ignore

        |> should throwAsyncExact typeof<System.ArgumentException>

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-sum returns sum of 1..10`` variant = task {
        // items are 1..10; sum = 55
        let! result = Gen.getSeqImmutable variant |> TaskSeq.sum
        result |> should equal 55
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-sumBy returns sum of id 1..10`` variant = task {
        let! result = Gen.getSeqImmutable variant |> TaskSeq.sumBy id
        result |> should equal 55
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-sumBy with projection returns sum of doubled values`` variant = task {
        // sum of 2*i for i in 1..10 = 2 * 55 = 110
        let! result = Gen.getSeqImmutable variant |> TaskSeq.sumBy ((*) 2)
        result |> should equal 110
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-sumByAsync with async projection returns sum`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.sumByAsync (fun x -> task { return x * 3 })

        // 3 * 55 = 165
        result |> should equal 165
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-average returns average of 1..10 as float`` variant = task {
        // items are 1..10; average = 5.5
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.map float
            |> TaskSeq.average

        result |> should (equalWithin 0.001) 5.5
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-averageBy returns average of float projections`` variant = task {
        // average of float values 1.0..10.0 = 5.5
        let! result = Gen.getSeqImmutable variant |> TaskSeq.averageBy float
        result |> should (equalWithin 0.001) 5.5
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-averageBy with custom projection returns correct average`` variant = task {
        // sum of 2*i / count = 2 * 5.5 = 11.0
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.averageBy (float >> (*) 2.0)

        result |> should (equalWithin 0.001) 11.0
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-averageByAsync with async projection returns correct average`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.averageByAsync (fun x -> task { return float x })

        result |> should (equalWithin 0.001) 5.5
    }

    [<Fact>]
    let ``TaskSeq-sum works with a single element`` () = task {
        let! result = TaskSeq.singleton 42 |> TaskSeq.sum
        result |> should equal 42
    }

    [<Fact>]
    let ``TaskSeq-average works with a single element`` () = task {
        let! result = TaskSeq.singleton 42.0 |> TaskSeq.average
        result |> should (equalWithin 0.001) 42.0
    }

    [<Fact>]
    let ``TaskSeq-sumBy works with float projection`` () = task {
        let! result = TaskSeq.ofSeq [ 1; 2; 3; 4; 5 ] |> TaskSeq.sumBy float

        result |> should (equalWithin 0.001) 15.0
    }

    [<Fact>]
    let ``TaskSeq-sum works with int64`` () = task {
        let! result = TaskSeq.ofSeq [ 1L; 2L; 3L; 4L; 5L ] |> TaskSeq.sum

        result |> should equal 15L
    }

    [<Fact>]
    let ``TaskSeq-average works with float32`` () = task {
        let! result = TaskSeq.ofSeq [ 1.0f; 2.0f; 3.0f ] |> TaskSeq.average

        result |> should (equalWithin 0.001f) 2.0f
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-sum iterates exactly once`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! result = ts |> TaskSeq.sum
        result |> should equal 55
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-sumBy iterates exactly once`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! result = ts |> TaskSeq.sumBy id
        result |> should equal 55
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-averageBy iterates exactly once`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! result = ts |> TaskSeq.averageBy float
        result |> should (equalWithin 0.001) 5.5
    }
