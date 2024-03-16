module TaskSeq.Tests.InsertAt

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control


//
// TaskSeq.insertAt
// TaskSeq.insertManyAt
//

exception SideEffectPastEnd of string

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-insertAt(0) on empty input returns singleton`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.insertAt 0 42
        |> verifySingleton 42

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-insertAt(1) on empty input should throw ArgumentException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.insertAt 1 42
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-insertAt(-1) should throw ArgumentException on any input`` () =
        fun () ->
            TaskSeq.empty<int>
            |> TaskSeq.insertAt -1 42
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            TaskSeq.init 10 id
            |> TaskSeq.insertAt -1 42
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-insertAt(-1) should throw ArgumentException before awaiting`` () =
        fun () ->
            taskSeq {
                do! longDelay ()

                if false then
                    yield 0 // type inference
            }
            |> TaskSeq.insertAt -1 42
            |> ignore // throws even without running the async. Bad coding, don't ignore a task!

        // test without awaiting the async
        |> should throw typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-insertManyAt(0) on empty input returns singleton`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.insertManyAt 0 (TaskSeq.ofArray [| 42; 43; 44 |])
        |> verifyDigitsAsString "jkl"

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-insertManyAt(1) on empty input should throw InvalidOperation`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.insertManyAt 1 (TaskSeq.ofArray [| 42; 43; 44 |])
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-insertManyAt(-1) should throw ArgumentException on any input`` () =
        fun () ->
            TaskSeq.empty<int>
            |> TaskSeq.insertManyAt -1 (TaskSeq.ofArray [| 42; 43; 44 |])
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            TaskSeq.init 10 id
            |> TaskSeq.insertManyAt -1 (TaskSeq.ofArray [| 42; 43; 44 |])
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-insertManyAt(-1) should throw ArgumentException before awaiting`` () =
        fun () ->
            taskSeq {
                do! longDelay ()

                if false then
                    yield 0 // type inference
            }
            |> TaskSeq.insertManyAt -1 (TaskSeq.ofArray [| 42; 43; 44 |])
            |> ignore // throws even without running the async. Bad coding, don't ignore a task!

        // test without awaiting the async
        |> should throw typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-insertManyAt() with empty sequenc as source`` () =
        TaskSeq.empty<int>
        |> TaskSeq.insertManyAt 0 TaskSeq.empty
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-insertManyAt() with empty sequence as source applies to non-empty sequence`` () =
        TaskSeq.init 10 id
        |> TaskSeq.insertManyAt 2 TaskSeq.empty
        |> verify0To9

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertAt can insert after end of sequence`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 10 99
            |> verifyDigitsAsString "ABCDEFGHIJ£"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertAt inserts item immediately after the indexed position`` variant = task {

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 0 99
            |> verifyDigitsAsString "£ABCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 1 99
            |> verifyDigitsAsString "A£BCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 5 99
            |> verifyDigitsAsString "ABCDE£FGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 10 99
            |> verifyDigitsAsString "ABCDEFGHIJ£"
    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertAt can be repeated in a chain`` variant = task {

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 0 99
            |> TaskSeq.insertAt 0 99
            |> TaskSeq.insertAt 0 99
            |> TaskSeq.insertAt 0 99
            |> TaskSeq.insertAt 0 99
            |> verifyDigitsAsString "£££££ABCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 10 99
            |> TaskSeq.insertAt 11 99
            |> TaskSeq.insertAt 12 99
            |> TaskSeq.insertAt 13 99
            |> TaskSeq.insertAt 14 99
            |> verifyDigitsAsString "ABCDEFGHIJ£££££"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertAt applies to a position in the new sequence`` variant = task {

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 0 99
            |> TaskSeq.insertAt 2 99
            |> TaskSeq.insertAt 4 99
            |> TaskSeq.insertAt 6 99
            |> TaskSeq.insertAt 8 99
            |> TaskSeq.insertAt 10 99
            |> TaskSeq.insertAt 12 99
            |> TaskSeq.insertAt 14 99
            |> TaskSeq.insertAt 16 99
            |> TaskSeq.insertAt 18 99
            |> TaskSeq.insertAt 20 99
            |> verifyDigitsAsString "£A£B£C£D£E£F£G£H£I£J£"
    }

    [<Fact>]
    let ``TaskSeq-insertAt can be applied to an infinite task sequence`` () =
        TaskSeq.initInfinite id
        |> TaskSeq.insertAt 100 12345
        |> TaskSeq.item 100
        |> Task.map (should equal 12345)


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertAt throws when there are not enough elements`` variant =
        fun () ->
            TaskSeq.singleton 1
            // insert after 1
            |> TaskSeq.insertAt 2 99
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 11 99
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.insertAt 10_000_000 99
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertManyAt can insert after end of sequence`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 10 (TaskSeq.ofArray [| 99; 100; 101 |])
            |> verifyDigitsAsString "ABCDEFGHIJ£¤¥"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertManyAt inserts item immediately after the indexed position`` variant = task {
        let values = TaskSeq.ofArray [| 99; 100; 101 |]

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 0 values
            |> verifyDigitsAsString "£¤¥ABCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 1 values
            |> verifyDigitsAsString "A£¤¥BCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 5 values
            |> verifyDigitsAsString "ABCDE£¤¥FGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 10 values
            |> verifyDigitsAsString "ABCDEFGHIJ£¤¥"
    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertManyAt can be repeated in a chain`` variant = task {
        let values = TaskSeq.ofArray [| 99; 100; 101 |]

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 0 values
            |> TaskSeq.insertManyAt 0 values
            |> TaskSeq.insertManyAt 0 values
            |> TaskSeq.insertManyAt 0 values
            |> TaskSeq.insertManyAt 0 values
            |> verifyDigitsAsString "£¤¥£¤¥£¤¥£¤¥£¤¥ABCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 10 values
            |> TaskSeq.insertManyAt 11 values
            |> TaskSeq.insertManyAt 12 values
            |> TaskSeq.insertManyAt 13 values
            |> TaskSeq.insertManyAt 14 values
            |> verifyDigitsAsString "ABCDEFGHIJ£££££¤¥¤¥¤¥¤¥¤¥"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertManyAt applies to a position in the new sequence`` variant = task {
        let values = TaskSeq.ofArray [| 99; 100; 101 |]

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 0 values
            |> TaskSeq.insertManyAt 4 values
            |> TaskSeq.insertManyAt 8 values
            |> TaskSeq.insertManyAt 12 values
            |> TaskSeq.insertManyAt 16 values
            |> TaskSeq.insertManyAt 20 values
            |> TaskSeq.insertManyAt 24 values
            |> TaskSeq.insertManyAt 28 values
            |> TaskSeq.insertManyAt 32 values
            |> TaskSeq.insertManyAt 36 values
            |> TaskSeq.insertManyAt 40 values
            |> verifyDigitsAsString "£¤¥A£¤¥B£¤¥C£¤¥D£¤¥E£¤¥F£¤¥G£¤¥H£¤¥I£¤¥J£¤¥"
    }

    [<Fact>]
    let ``TaskSeq-insertManyAt (infinite) can be applied to an infinite task sequence`` () =
        TaskSeq.initInfinite id
        |> TaskSeq.insertManyAt 100 (TaskSeq.init 10 id)
        |> TaskSeq.item 109
        |> Task.map (should equal 9)



    [<Fact>]
    let ``TaskSeq-insertManyAt (infinite) with infinite task sequence as argument`` () =
        TaskSeq.init 100 id
        |> TaskSeq.insertManyAt 100 (TaskSeq.initInfinite id)
        |> TaskSeq.item 1999
        |> Task.map (should equal 1899) // the inserted infinite sequence started at 100, with value 0.

    [<Fact>]
    let ``TaskSeq-insertManyAt (infinite) with source and values both as infinite task sequence`` () = task {

        // using two infinite task sequences
        let ts =
            TaskSeq.initInfinite id
            |> TaskSeq.insertManyAt 1000 (TaskSeq.initInfinite id)

        // the inserted infinite sequence started at 1000, with value 0.
        do! ts |> TaskSeq.item 999 |> Task.map (should equal 999)
        do! ts |> TaskSeq.item 1000 |> Task.map (should equal 0)
        do! ts |> TaskSeq.item 2000 |> Task.map (should equal 1000)
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-insertManyAt throws when there are not enough elements`` variant =
        let values = TaskSeq.ofArray [| 99; 100; 101 |]

        fun () ->
            TaskSeq.singleton 1
            // insert after 1
            |> TaskSeq.insertManyAt 2 values
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 11 values
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.insertManyAt 10_000_000 values
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>



module SideEffects =

    // PoC test
    [<Fact>]
    let ``Seq-insertAt (poc-proof) will execute side effect before index`` () =
        // NOTE: this test is for documentation purposes only, to show this behavior that is tested in this module
        // this shows that Seq.insertAt executes more side effects than necessary.

        let mutable x = 42

        let items = seq {
            x <- x + 1 // we are proving this gets executed with insertAt(0)
            yield x
            yield x * 2
        }

        items
        |> Seq.insertAt 0 99
        |> Seq.item 0 // put enumerator to inserted item
        |> ignore

        x |> should equal 43 // one time side effect executed. QED

    [<Fact>]
    let ``TaskSeq-insertAt(0) will execute side effects at start of sequence`` () =
        // NOTE: while not strictly necessary, this mirrors behavior of Seq.insertAt

        let mutable x = 42 // for this test, the potential mutation should not actually occur

        let items = taskSeq {
            x <- x + 1 // this is executed even with insertAt(0)
            yield x
            yield x * 2
        }

        items
        |> TaskSeq.insertAt 0 99
        |> TaskSeq.item 0 // consume only the first item
        |> Task.map (should equal 99)
        |> Task.map (fun () -> x |> should equal 43) // the mutable was updated

    [<Fact>]
    let ``TaskSeq-insertAt will execute last side effect when inserting past end`` () =
        let mutable x = 42

        let items = taskSeq {
            yield x
            yield x * 2
            yield x * 4
            x <- x + 1 // this is executed when inserting past last item
        }

        items
        |> TaskSeq.insertAt 3 99
        |> TaskSeq.item 3
        |> Task.map (should equal 99)
        |> Task.map (fun () -> x |> should equal 43) // as with 'seq', see first test in this block, we execute the side effect at index


    [<Fact>]
    let ``TaskSeq-insertAt will execute side effect just before index`` () =
        let mutable x = 42

        let items = taskSeq {
            yield x
            x <- x + 1 // this is executed, even though we insert after the first item
            yield x * 2
            yield x * 4
        }

        items
        |> TaskSeq.insertAt 1 99
        |> TaskSeq.item 1
        |> Task.map (should equal 99)
        |> Task.map (fun () -> x |> should equal 43) // as with 'seq', see first test in this block, we execute the side effect at index

    [<Fact>]
    let ``TaskSeq-insertAt exception at insertion index is thrown`` () =
        fun () ->
            taskSeq {
                yield 1
                yield! [ 2; 3 ]
                do SideEffectPastEnd "at the end" |> raise // this is raised
                yield 4
            }
            |> TaskSeq.insertAt 3 99
            |> TaskSeq.item 3
            |> Task.ignore

        |> should throwAsyncExact typeof<SideEffectPastEnd>

    [<Fact>]
    let ``TaskSeq-insertAt prove that an exception from the taskSeq is thrown instead of exception from function`` () =
        let items = taskSeq {
            yield 42
            yield! [ 1; 2 ]
            do SideEffectPastEnd "at the end" |> raise // we SHOULD get here before ArgumentException is raised
        }

        fun () -> items |> TaskSeq.insertAt 4 99 |> consumeTaskSeq // this would raise ArgumentException normally, but not now
        |> should throwAsyncExact typeof<SideEffectPastEnd>

    [<Fact>]
    let ``TaskSeq-insertManyAt(0) will execute side effects at start of sequence`` () =
        // NOTE: while not strictly necessary, this mirrors behavior of Seq.insertManyAt

        let mutable x = 42 // for this test, the potential mutation should not actually occur

        let items = taskSeq {
            x <- x + 1 // this is executed even with insertManyAt(0)
            yield x
            yield x * 2
        }

        items
        |> TaskSeq.insertManyAt 0 (taskSeq { yield! [ 99; 100 ] })
        |> TaskSeq.item 0 // consume only the first item
        |> Task.map (should equal 99)
        |> Task.map (fun () -> x |> should equal 43) // the mutable was updated

    [<Fact>]
    let ``TaskSeq-insertManyAt will execute last side effect when inserting past end`` () =
        let mutable x = 42

        let items = taskSeq {
            yield x
            yield x * 2
            yield x * 4
            x <- x + 1 // this is executed when inserting past last item
        }

        items
        |> TaskSeq.insertManyAt 3 (taskSeq { yield! [ 99; 100 ] })
        |> TaskSeq.item 3
        |> Task.map (should equal 99)
        |> Task.map (fun () -> x |> should equal 43) // as with 'seq', see first test in this block, we execute the side effect at index


    [<Fact>]
    let ``TaskSeq-insertManyAt will execute side effect just before index`` () =
        let mutable x = 42

        let items = taskSeq {
            yield x
            x <- x + 1 // this is executed, even though we insert after the first item
            yield x * 2
            yield x * 4
        }

        items
        |> TaskSeq.insertManyAt 1 (taskSeq { yield! [ 99; 100 ] })
        |> TaskSeq.item 1
        |> Task.map (should equal 99)
        |> Task.map (fun () -> x |> should equal 43) // as with 'seq', see first test in this block, we execute the side effect at index

    [<Fact>]
    let ``TaskSeq-insertManyAt exception at insertion index is thrown`` () =
        fun () ->
            taskSeq {
                yield 1
                yield! [ 2; 3 ]
                do SideEffectPastEnd "at the end" |> raise // this is raised
                yield 4
            }
            |> TaskSeq.insertManyAt 3 (taskSeq { yield! [ 99; 100 ] })
            |> TaskSeq.item 3
            |> Task.ignore

        |> should throwAsyncExact typeof<SideEffectPastEnd>

    [<Fact>]
    let ``TaskSeq-insertManyAt prove that an exception from the taskSeq is thrown instead of exception from function`` () =
        let items = taskSeq {
            yield 42
            yield! [ 1; 2 ]
            do SideEffectPastEnd "at the end" |> raise // we SHOULD get here before ArgumentException is raised
        }

        fun () ->
            items
            |> TaskSeq.insertManyAt 4 (taskSeq { yield! [ 99; 100 ] })
            |> consumeTaskSeq // this would raise ArgumentException normally, but not now

        |> should throwAsyncExact typeof<SideEffectPastEnd>
