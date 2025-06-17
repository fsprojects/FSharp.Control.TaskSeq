module TaskSeq.Tests.ChunkBySize

open System

open FsUnitTyped
open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.chunkBySize
//

exception SideEffectPastEnd of string

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chunkBySize(0) on empty input should throw InvalidOperation`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.chunkBySize 0
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chunkBySize(1) has no effect on empty input`` variant =
        // no `task` block needed
        Gen.getEmptyVariant variant
        |> TaskSeq.chunkBySize 1
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chunkBySize(99) has no effect on empty input`` variant =
        // no `task` block needed
        Gen.getEmptyVariant variant
        |> TaskSeq.chunkBySize 99
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-chunkBySize(-1) should throw ArgumentException on any input`` () =
        fun () ->
            TaskSeq.empty<int>
            |> TaskSeq.chunkBySize -1
            |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            TaskSeq.init 10 id
            |> TaskSeq.chunkBySize -1
            |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-chunkBySize(-1) should throw ArgumentException before awaiting`` () =
        fun () ->
            taskSeq {
                do! longDelay ()

                if false then
                    yield 0 // type inference
            }
            |> TaskSeq.chunkBySize -1
            |> ignore // throws even without running the async. Bad coding, don't ignore a task!

        |> should throw typeof<ArgumentException>

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize returns all items from source in order`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 3
            |> TaskSeq.collect TaskSeq.ofArray
            |> verify1To10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize returns chunks with items in order`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 2
            |> TaskSeq.toArrayAsync
            |> Task.map (shouldEqual [| [| 1; 2 |]; [| 3; 4 |]; [| 5; 6 |]; [| 7; 8 |]; [| 9; 10 |] |])
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize returns exactly 'chunkSize' items per chunk`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 1
            |> TaskSeq.iter (shouldHaveLength 1)

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 2
            |> TaskSeq.iter (shouldHaveLength 2)

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize 5
            |> TaskSeq.iter (shouldHaveLength 5)
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize returns remaining items in last chunk`` variant = task {
        let verifyChunk chunkSize lastChunkSize =
            Gen.getSeqImmutable variant
            |> TaskSeq.chunkBySize chunkSize
            |> TaskSeq.toArrayAsync
            |> Task.map (Array.last >> shouldHaveLength lastChunkSize)

        do! verifyChunk 1 1
        do! verifyChunk 3 1
        do! verifyChunk 4 2
        do! verifyChunk 6 4
        do! verifyChunk 7 3
        do! verifyChunk 8 2
        do! verifyChunk 9 1
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chunkBySize returns all elements when 'chunkSize' > number of items`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.chunkBySize 11
        |> TaskSeq.toArrayAsync
        |> Task.map (Array.exactlyOne >> shouldHaveLength 10)

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chunkBySize gets all items`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.chunkBySize 5
        |> TaskSeq.toArrayAsync
        |> Task.map (shouldEqual [| [| 1..5 |]; [| 6..10 |] |])

    [<Fact>]
    let ``TaskSeq-chunkBySize prove we execute empty-seq side-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.chunkBySize 1 |> consumeTaskSeq
        do! ts |> TaskSeq.chunkBySize 2 |> consumeTaskSeq
        do! ts |> TaskSeq.chunkBySize 3 |> consumeTaskSeq
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-chunkBySize prove we execute after-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield 42
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.chunkBySize 1 |> consumeTaskSeq
        do! ts |> TaskSeq.chunkBySize 2 |> consumeTaskSeq
        do! ts |> TaskSeq.chunkBySize 3 |> consumeTaskSeq
        i |> should equal 9
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chunkBySize should go over all items`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        do! ts |> TaskSeq.chunkBySize 1 |> consumeTaskSeq
        do! ts |> TaskSeq.chunkBySize 2 |> consumeTaskSeq
        do! ts |> TaskSeq.chunkBySize 3 |> consumeTaskSeq
        // incl. the iteration of 'last', we reach 40
        do! ts |> TaskSeq.last |> Task.map (should equal 40)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chunkBySize multiple iterations over same sequence`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let mutable sum = 0

        do!
            TaskSeq.chunkBySize 1 ts
            |> TaskSeq.collect TaskSeq.ofArray
            |> TaskSeq.iter (fun item -> sum <- sum + item)

        do!
            TaskSeq.chunkBySize 2 ts
            |> TaskSeq.collect TaskSeq.ofArray
            |> TaskSeq.iter (fun item -> sum <- sum + item)

        do!
            TaskSeq.chunkBySize 3 ts
            |> TaskSeq.collect TaskSeq.ofArray
            |> TaskSeq.iter (fun item -> sum <- sum + item)

        do!
            TaskSeq.chunkBySize 4 ts
            |> TaskSeq.collect TaskSeq.ofArray
            |> TaskSeq.iter (fun item -> sum <- sum + item)

        sum |> should equal 820 // side-effected tasks, so 'item' DOES CHANGE, each next iteration starts 10 higher
    }

    [<Fact>]
    let ``TaskSeq-chunkBySize prove that an exception from the taskSeq is thrown`` () =
        let items = taskSeq {
            yield 42
            yield! [ 1; 2 ]
            do SideEffectPastEnd "at the end" |> raise
            yield 43
        }

        fun () -> items |> TaskSeq.chunkBySize 2 |> consumeTaskSeq
        |> should throwAsyncExact typeof<SideEffectPastEnd>
