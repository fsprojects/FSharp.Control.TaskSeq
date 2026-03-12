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
    let ``TaskSeq-sum works with float projection`` () = task {
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

    [<Fact>]
    let ``TaskSeq-sum result matches Seq-sum`` () = task {
        let items = [ 3; 1; 4; 1; 5; 9; 2; 6; 5; 3 ]
        let expected = Seq.sum items

        let! result = TaskSeq.ofList items |> TaskSeq.sum
        result |> should equal expected
    }

    [<Fact>]
    let ``TaskSeq-average result matches Seq-average`` () = task {
        let items = [ 3.0; 1.0; 4.0; 1.0; 5.0; 9.0; 2.0; 6.0; 5.0; 3.0 ]
        let expected = Seq.average items

        let! result = TaskSeq.ofList items |> TaskSeq.average
        result |> should (equalWithin 0.0001) expected
    }

    [<Fact>]
    let ``TaskSeq-sumBy result matches Seq-sumBy`` () = task {
        let items = [ 1; 2; 3; 4; 5 ]
        let expected = Seq.sumBy (fun x -> x * x) items

        let! result = TaskSeq.ofList items |> TaskSeq.sumBy (fun x -> x * x)
        result |> should equal expected
    }

    [<Fact>]
    let ``TaskSeq-averageBy result matches Seq-averageBy`` () = task {
        let items = [ 1; 2; 3; 4; 5 ]
        let expected = Seq.averageBy float items

        let! result = TaskSeq.ofList items |> TaskSeq.averageBy float
        result |> should (equalWithin 0.0001) expected
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
    let ``TaskSeq-sumByAsync iterates exactly once`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! result = ts |> TaskSeq.sumByAsync Task.fromResult
        result |> should equal 55
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-average iterates exactly once`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! result = ts |> TaskSeq.map float |> TaskSeq.average
        result |> should (equalWithin 0.001) 5.5
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-averageBy iterates exactly once`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! result = ts |> TaskSeq.averageBy float
        result |> should (equalWithin 0.001) 5.5
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-averageByAsync iterates exactly once`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! result = ts |> TaskSeq.averageByAsync (float >> Task.fromResult)
        result |> should (equalWithin 0.001) 5.5
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-sum second iteration sees side-effect values`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! first = ts |> TaskSeq.sum
        first |> should equal 55 // 1+2+...+10

        // side-effect sequences yield next 10 items (11..20) on second consumption
        let! second = ts |> TaskSeq.sum
        second |> should equal 155 // 11+12+...+20
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-averageBy second iteration sees side-effect values`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! first = ts |> TaskSeq.averageBy float
        first |> should (equalWithin 0.001) 5.5 // avg(1..10) = 5.5

        // side-effect sequences yield next 10 items (11..20) on second consumption
        let! second = ts |> TaskSeq.averageBy float
        second |> should (equalWithin 0.001) 15.5 // avg(11..20) = 15.5
    }
