module TaskSeq.Tests.ChunkBySize

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.chunkBySize
//

module EmptySeq =
    [<Fact>]
    let ``TaskSeq-chunkBySize with null source raises`` () = assertNullArg <| fun () -> TaskSeq.chunkBySize 1 null

    [<Fact>]
    let ``TaskSeq-chunkBySize with zero raises ArgumentException before awaiting`` () =
        fun () -> TaskSeq.empty<int> |> TaskSeq.chunkBySize 0 |> ignore // throws eagerly, before enumeration
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``TaskSeq-chunkBySize with negative raises ArgumentException before awaiting`` () =
        fun () -> TaskSeq.empty<int> |> TaskSeq.chunkBySize -1 |> ignore
        |> should throw typeof<System.ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chunkBySize on empty sequence yields empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.chunkBySize 1
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chunkBySize(99) on empty sequence yields empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.chunkBySize 99
        |> verifyEmpty

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize preserves all elements in order`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 3
            |> TaskSeq.collect TaskSeq.ofArray
            |> verify1To10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize(2) returns 5 chunks of 2 for a 10-element sequence`` variant = task {
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 2
            |> TaskSeq.toArrayAsync

        chunks
        |> should equal [| [| 1; 2 |]; [| 3; 4 |]; [| 5; 6 |]; [| 7; 8 |]; [| 9; 10 |] |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize(5) returns 2 full chunks for a 10-element sequence`` variant = task {
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 5
            |> TaskSeq.toArrayAsync

        chunks |> should equal [| [| 1..5 |]; [| 6..10 |] |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize(1) returns each element as its own array`` variant = task {
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 1
            |> TaskSeq.toArrayAsync

        chunks |> Array.length |> should equal 10

        chunks
        |> Array.iteri (fun i chunk -> chunk |> should equal [| i + 1 |])
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize last chunk contains remainder when sequence does not divide evenly`` variant = task {
        // 10 elements with chunk size 3 → chunks [1;2;3] [4;5;6] [7;8;9] [10]
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 3
            |> TaskSeq.toArrayAsync

        chunks |> Array.length |> should equal 4
        chunks |> Array.last |> should equal [| 10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize larger than sequence returns single chunk with all elements`` variant = task {
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 11
            |> TaskSeq.toArrayAsync

        chunks |> Array.length |> should equal 1
        chunks.[0] |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize equal to sequence length returns single full chunk`` variant = task {
        let! chunks =
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 10
            |> TaskSeq.toArrayAsync

        chunks |> Array.length |> should equal 1
        chunks.[0] |> should equal [| 1..10 |]
    }

    [<Fact>]
    let ``TaskSeq-chunkBySize each chunk array is independent - modifying one does not affect others`` () = task {
        let! chunks =
            taskSeq { yield! [ 1..6 ] }
            |> TaskSeq.chunkBySize 3
            |> TaskSeq.toArrayAsync

        // Mutate the first chunk
        chunks.[0].[0] <- 99

        // The second chunk must be unaffected
        chunks.[1] |> should equal [| 4; 5; 6 |]
        chunks.[0].[0] |> should equal 99
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize remainder sizes`` variant = task {
        let verifyLastChunkSize chunkSize expectedLast =
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize chunkSize
            |> TaskSeq.toArrayAsync
            |> Task.map (Array.last >> Array.length >> should equal expectedLast)

        do! verifyLastChunkSize 3 1 // 10 mod 3 = 1
        do! verifyLastChunkSize 4 2 // 10 mod 4 = 2
        do! verifyLastChunkSize 6 4 // 10 mod 6 = 4
        do! verifyLastChunkSize 7 3 // 10 mod 7 = 3
        do! verifyLastChunkSize 9 1 // 10 mod 9 = 1
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chunkBySize gets all items`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.chunkBySize 5
        |> TaskSeq.toArrayAsync
        |> Task.map (should equal [| [| 1..5 |]; [| 6..10 |] |])

    [<Fact>]
    let ``TaskSeq-chunkBySize executes side-effects from empty source`` () = task {
        let mutable sideEffects = 0

        let ts = taskSeq {
            sideEffects <- sideEffects + 1
            sideEffects <- sideEffects + 1
        }

        do! ts |> TaskSeq.chunkBySize 1 |> consumeTaskSeq
        do! ts |> TaskSeq.chunkBySize 3 |> consumeTaskSeq
        sideEffects |> should equal 4
    }

    [<Fact>]
    let ``TaskSeq-chunkBySize executes all source side-effects`` () = task {
        let mutable sideEffects = 0

        let ts = taskSeq {
            sideEffects <- sideEffects + 1
            yield 1
            sideEffects <- sideEffects + 1
            yield 2
            sideEffects <- sideEffects + 1 // executed even after last yield
        }

        do! ts |> TaskSeq.chunkBySize 2 |> consumeTaskSeq
        sideEffects |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-chunkBySize propagates exception from source`` () =
        let items = taskSeq {
            yield 1
            yield 2
            failwith "boom"
            yield 3
        }

        fun () -> items |> TaskSeq.chunkBySize 2 |> consumeTaskSeq
        |> should throwAsyncExact typeof<System.Exception>
