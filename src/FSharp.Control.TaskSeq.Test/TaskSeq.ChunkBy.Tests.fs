module TaskSeq.Tests.ChunkBy

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.chunkBy
// TaskSeq.chunkByAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.chunkBy id (null: TaskSeq<int>)

        assertNullArg
        <| fun () -> TaskSeq.chunkByAsync (fun x -> Task.fromResult x) (null: TaskSeq<int>)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chunkBy on empty gives empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.chunkBy id
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chunkByAsync on empty gives empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.chunkByAsync (fun x -> Task.fromResult x)
        |> verifyEmpty


module Functionality =
    [<Fact>]
    let ``TaskSeq-chunkBy groups consecutive equal elements`` () = task {
        let ts = taskSeq { yield! [ 1; 1; 2; 2; 2; 3 ] }
        let! result = TaskSeq.chunkBy id ts |> TaskSeq.toArrayAsync
        result |> should haveLength 3
        result[0] |> should equal (1, [| 1; 1 |])
        result[1] |> should equal (2, [| 2; 2; 2 |])
        result[2] |> should equal (3, [| 3 |])
    }

    [<Fact>]
    let ``TaskSeq-chunkBy with all same key yields one chunk`` () = task {
        let ts = taskSeq { yield! [ 5; 5; 5; 5 ] }
        let! result = TaskSeq.chunkBy id ts |> TaskSeq.toArrayAsync
        result |> should haveLength 1
        result[0] |> should equal (5, [| 5; 5; 5; 5 |])
    }

    [<Fact>]
    let ``TaskSeq-chunkBy with all different keys yields singleton chunks`` () = task {
        let ts = taskSeq { yield! [ 1..5 ] }
        let! result = TaskSeq.chunkBy id ts |> TaskSeq.toArrayAsync
        result |> should haveLength 5

        result
        |> Array.iteri (fun i (k, arr) ->
            k |> should equal (i + 1)
            arr |> should equal [| i + 1 |])
    }

    [<Fact>]
    let ``TaskSeq-chunkBy with singleton source yields one chunk`` () = task {
        let ts = TaskSeq.singleton 42
        let! result = TaskSeq.chunkBy id ts |> TaskSeq.toArrayAsync
        result |> should haveLength 1
        result[0] |> should equal (42, [| 42 |])
    }

    [<Fact>]
    let ``TaskSeq-chunkBy uses projection key, not element`` () = task {
        let ts = taskSeq {
            yield "a1"
            yield "a2"
            yield "b1"
            yield "b2"
            yield "a3"
        }

        let! result =
            TaskSeq.chunkBy (fun (s: string) -> s[0]) ts
            |> TaskSeq.toArrayAsync

        result |> should haveLength 3
        let k0, arr0 = result[0]
        k0 |> should equal 'a'
        arr0 |> should equal [| "a1"; "a2" |]
        let k1, arr1 = result[1]
        k1 |> should equal 'b'
        arr1 |> should equal [| "b1"; "b2" |]
        let k2, arr2 = result[2]
        k2 |> should equal 'a'
        arr2 |> should equal [| "a3" |]
    }

    [<Fact>]
    let ``TaskSeq-chunkBy does not merge non-consecutive equal keys`` () = task {
        // Key alternates: 1, 2, 1, 2 — should produce 4 chunks not 2
        let ts = taskSeq { yield! [ 1; 2; 1; 2 ] }
        let! result = TaskSeq.chunkBy id ts |> TaskSeq.toArrayAsync
        result |> should haveLength 4
    }

    [<Fact>]
    let ``TaskSeq-chunkByAsync groups consecutive by async key`` () = task {
        let ts = taskSeq { yield! [ 1; 1; 2; 3; 3 ] }

        let! result =
            TaskSeq.chunkByAsync (fun x -> Task.fromResult (x % 2 = 0)) ts
            |> TaskSeq.toArrayAsync
        // odd, even, odd -> 3 chunks
        result |> should haveLength 3
        let k0, arr0 = result[0]
        k0 |> should equal false
        arr0 |> should equal [| 1; 1 |]
        let k1, arr1 = result[1]
        k1 |> should equal true
        arr1 |> should equal [| 2 |]
        let k2, arr2 = result[2]
        k2 |> should equal false
        arr2 |> should equal [| 3; 3 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBy all elements same key as variants`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! result = TaskSeq.chunkBy (fun _ -> 0) ts |> TaskSeq.toArrayAsync
        result |> should haveLength 1
        let _, arr = result[0]
        arr |> should haveLength 10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBy each element its own chunk as variants`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! result = TaskSeq.chunkBy id ts |> TaskSeq.toArrayAsync
        result |> should haveLength 10

        result
        |> Array.iteri (fun i (k, arr) ->
            k |> should equal (i + 1)
            arr |> should haveLength 1
            arr[0] |> should equal (i + 1))
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkByAsync all elements same key as variants`` variant = task {
        let ts = Gen.getSeqImmutable variant

        let! result =
            TaskSeq.chunkByAsync (fun _ -> Task.fromResult 0) ts
            |> TaskSeq.toArrayAsync

        result |> should haveLength 1
        let _, arr = result[0]
        arr |> should haveLength 10
    }


module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chunkBy on side-effect seq groups all elements under one key`` variant = task {
        let! result =
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.chunkBy (fun _ -> 0)
            |> TaskSeq.toArrayAsync

        result |> should haveLength 1
        let _, arr = result[0]
        arr |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chunkByAsync on side-effect seq groups all elements under one key`` variant = task {
        let! result =
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.chunkByAsync (fun _ -> Task.fromResult 0)
            |> TaskSeq.toArrayAsync

        result |> should haveLength 1
        let _, arr = result[0]
        arr |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chunkBy on side-effect seq produces correct singleton chunks`` variant = task {
        let! result =
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.chunkBy id
            |> TaskSeq.toArrayAsync

        result |> should haveLength 10

        result
        |> Array.iteri (fun i (k, arr) ->
            k |> should equal (i + 1)
            arr |> should equal [| i + 1 |])
    }

    [<Fact>]
    let ``TaskSeq-chunkBy projection is called exactly once per element`` () = task {
        let mutable callCount = 0

        let ts = taskSeq { yield! [ 1; 1; 2; 3; 3 ] }

        let! result =
            ts
            |> TaskSeq.chunkBy (fun x ->
                callCount <- callCount + 1
                x % 2)
            |> TaskSeq.toArrayAsync

        callCount |> should equal 5
        result |> should haveLength 3
    }

    [<Fact>]
    let ``TaskSeq-chunkByAsync projection is called exactly once per element`` () = task {
        let mutable callCount = 0

        let ts = taskSeq { yield! [ 1; 1; 2; 3; 3 ] }

        let! result =
            ts
            |> TaskSeq.chunkByAsync (fun x -> task {
                callCount <- callCount + 1
                return x % 2
            })
            |> TaskSeq.toArrayAsync

        callCount |> should equal 5
        result |> should haveLength 3
    }

    [<Fact>]
    let ``TaskSeq-chunkBy does not evaluate elements before enumeration`` () = task {
        let mutable sourceCount = 0

        let ts = taskSeq {
            for i in 1..5 do
                sourceCount <- sourceCount + 1
                yield i
        }

        let chunked = TaskSeq.chunkBy (fun x -> x % 2 = 0) ts
        // Building the pipeline does not consume the source
        sourceCount |> should equal 0

        let! _ = TaskSeq.toArrayAsync chunked
        // Only after consuming does the source get evaluated
        sourceCount |> should equal 5
    }
