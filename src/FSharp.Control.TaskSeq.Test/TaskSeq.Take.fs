module TaskSeq.Tests.Take

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.take
// TaskSeq.truncate
//

exception SideEffectPastEnd of string

[<AutoOpen>]
module With =
    /// Turns a sequence of numbers into a string, starting with A for '1'
    let verifyAsString expected =
        TaskSeq.map char
        >> TaskSeq.map ((+) '@')
        >> TaskSeq.toArrayAsync
        >> Task.map (String >> should equal expected)

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-take(0) has no effect on empty input`` variant =
        // no `task` block needed
        Gen.getEmptyVariant variant |> TaskSeq.take 0 |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-take(1) on empty input should throw InvalidOperation`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.take 1
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-take(-1) should throw ArgumentException on any input`` () =
        fun () -> TaskSeq.empty<int> |> TaskSeq.take -1 |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

        fun () -> TaskSeq.init 10 id |> TaskSeq.take -1 |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-take(-1) should throw ArgumentException before awaiting`` () =
        fun () ->
            taskSeq {
                do! longDelay ()

                if false then
                    yield 0 // type inference
            }
            |> TaskSeq.take -1
            |> ignore // throws even without running the async. Bad coding, don't ignore a task!

        |> should throw typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-truncate(0) has no effect on empty input`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.truncate 0
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-truncate(99) does not throw on empty input`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.truncate 99
        |> verifyEmpty


    [<Fact>]
    let ``TaskSeq-truncate(-1) should throw ArgumentException on any input`` () =
        fun () -> TaskSeq.empty<int> |> TaskSeq.truncate -1 |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

        fun () -> TaskSeq.init 10 id |> TaskSeq.truncate -1 |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-truncate(-1) should throw ArgumentException before awaiting`` () =
        fun () ->
            taskSeq {
                do! longDelay ()

                if false then
                    yield 0 // type inference
            }
            |> TaskSeq.truncate -1
            |> ignore // throws even without running the async. Bad coding, don't ignore a task!

        |> should throw typeof<ArgumentException>

module Immutable =

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-take returns exactly 'count' items`` variant = task {

        do! Gen.getSeqImmutable variant |> TaskSeq.take 0 |> verifyEmpty

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.take 1
            |> verifyAsString "A"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.take 5
            |> verifyAsString "ABCDE"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.take 10
            |> verifyAsString "ABCDEFGHIJ"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-take throws when there are not enough elements`` variant =
        fun () -> TaskSeq.init 1 id |> TaskSeq.take 2 |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.take 11
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.take 10_000_000
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-truncate returns at least 'count' items`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.truncate 0
            |> verifyEmpty

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.truncate 1
            |> verifyAsString "A"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.truncate 5
            |> verifyAsString "ABCDE"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.truncate 10
            |> verifyAsString "ABCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.truncate 11
            |> verifyAsString "ABCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.truncate 10_000_000
            |> verifyAsString "ABCDEFGHIJ"
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-take gets enough items`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.take 5
        |> verifyAsString "ABCDE"

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-truncate gets enough items`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.truncate 5
        |> verifyAsString "ABCDE"

    [<Fact>]
    let ``TaskSeq-take prove it does not read beyond the last yield`` () = task {
        let mutable x = 42 // for this test, the potential mutation should not actually occur

        let items = taskSeq {
            yield x
            yield x * 2
            x <- x + 1 // we are proving we never get here
        }

        let expected = [| 42; 84 |]

        let! first = items |> TaskSeq.take 2 |> TaskSeq.toArrayAsync
        let! repeat = items |> TaskSeq.take 2 |> TaskSeq.toArrayAsync

        first |> should equal expected
        repeat |> should equal expected // if we read too far, this is now [|43, 86|]
        x |> should equal 42 // expect: side-effect at end of taskseq not executed
    }

    [<Fact>]
    let ``TaskSeq-take prove that an exception that is not consumed, is not raised`` () =
        let items = taskSeq {
            yield 1
            yield! [ 2; 3 ]
            do SideEffectPastEnd "at the end" |> raise // we SHOULD NOT get here
        }

        items |> TaskSeq.take 3 |> verifyAsString "ABC"


    [<Fact>]
    let ``TaskSeq-take prove that an exception from the taskseq is thrown instead of exception from function`` () =
        let items = taskSeq {
            yield 42
            yield! [ 1; 2 ]
            do SideEffectPastEnd "at the end" |> raise // we SHOULD get here before ArgumentException is raised
        }

        fun () -> items |> TaskSeq.take 4 |> consumeTaskSeq // this would raise ArgumentException normally
        |> should throwAsyncExact typeof<SideEffectPastEnd>


    [<Fact>]
    let ``TaskSeq-truncate prove it does not read beyond the last yield`` () = task {
        let mutable x = 42 // for this test, the potential mutation should not actually occur

        let items = taskSeq {
            yield x
            yield x * 2
            x <- x + 1 // we are proving we never get here
        }

        let expected = [| 42; 84 |]

        let! first = items |> TaskSeq.truncate 2 |> TaskSeq.toArrayAsync
        let! repeat = items |> TaskSeq.truncate 2 |> TaskSeq.toArrayAsync

        first |> should equal expected
        repeat |> should equal expected // if we read too far, this is now [|43, 86|]
        x |> should equal 42 // expect: side-effect at end of taskseq not executed
    }
