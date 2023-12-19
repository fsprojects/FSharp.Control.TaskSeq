module TaskSeq.Tests.Skip

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.skip
// TaskSeq.drop
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
    let ``TaskSeq-skip(0) has no effect on empty input`` variant =
        // no `task` block needed
        Gen.getEmptyVariant variant |> TaskSeq.skip 0 |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-skip(1) on empty input should throw InvalidOperation`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.skip 1
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-skip(-1) should throw ArgumentException on any input`` () =
        fun () -> TaskSeq.empty<int> |> TaskSeq.skip -1 |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

        fun () -> TaskSeq.init 10 id |> TaskSeq.skip -1 |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-skip(-1) should throw ArgumentException before awaiting`` () =
        fun () ->
            taskSeq {
                do! longDelay ()

                if false then
                    yield 0 // type inference
            }
            |> TaskSeq.skip -1
            |> ignore // throws even without running the async. Bad coding, don't ignore a task!

        |> should throw typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-drop(0) has no effect on empty input`` variant = Gen.getEmptyVariant variant |> TaskSeq.drop 0 |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-drop(99) does not throw on empty input`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.drop 99
        |> verifyEmpty


    [<Fact>]
    let ``TaskSeq-drop(-1) should throw ArgumentException on any input`` () =
        fun () -> TaskSeq.empty<int> |> TaskSeq.drop -1 |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

        fun () -> TaskSeq.init 10 id |> TaskSeq.drop -1 |> consumeTaskSeq
        |> should throwAsyncExact typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq-drop(-1) should throw ArgumentException before awaiting`` () =
        fun () ->
            taskSeq {
                do! longDelay ()

                if false then
                    yield 0 // type inference
            }
            |> TaskSeq.drop -1
            |> ignore // throws even without running the async. Bad coding, don't ignore a task!

        |> should throw typeof<ArgumentException>

module Immutable =

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-skip skips over exactly 'count' items`` variant = task {

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skip 0
            |> verifyAsString "ABCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skip 1
            |> verifyAsString "BCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skip 5
            |> verifyAsString "FGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skip 10
            |> verifyEmpty
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-skip throws when there are not enough elements`` variant =
        fun () -> TaskSeq.init 1 id |> TaskSeq.skip 2 |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.skip 11
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.skip 10_000_000
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-drop skips over at least 'count' items`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.drop 0
            |> verifyAsString "ABCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.drop 1
            |> verifyAsString "BCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.drop 5
            |> verifyAsString "FGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.drop 10
            |> verifyEmpty

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.drop 11 // no exception
            |> verifyEmpty

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.drop 10_000_000 // no exception
            |> verifyEmpty
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-skip skips over enough items`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.skip 5
        |> verifyAsString "FGHIJ"

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-drop skips over enough items`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.drop 5
        |> verifyAsString "FGHIJ"

    [<Fact>]
    let ``TaskSeq-skip prove we do not skip side effects`` () = task {
        let mutable x = 42 // for this test, the potential mutation should not actually occur

        let items = taskSeq {
            yield x
            yield x * 2
            x <- x + 1 // we are proving we never get here
        }

        let! first = items |> TaskSeq.skip 2 |> TaskSeq.toArrayAsync
        let! repeat = items |> TaskSeq.skip 2 |> TaskSeq.toArrayAsync

        first |> should equal Array.empty<int>
        repeat |> should equal Array.empty<int>
        x |> should equal 44 // expect: side-effect is executed twice by now
    }

    [<Fact>]
    let ``TaskSeq-skip prove that an exception from the taskseq is thrown instead of exception from function`` () =
        let items = taskSeq {
            yield 42
            yield! [ 1; 2 ]
            do SideEffectPastEnd "at the end" |> raise // we SHOULD get here before ArgumentException is raised
        }

        fun () -> items |> TaskSeq.skip 4 |> consumeTaskSeq // this would raise ArgumentException normally
        |> should throwAsyncExact typeof<SideEffectPastEnd>


    [<Fact>]
    let ``TaskSeq-drop prove we do not skip side effects at the end`` () = task {
        let mutable x = 42 // for this test, the potential mutation should not actually occur

        let items = taskSeq {
            yield x
            yield x * 2
            x <- x + 1 // we are proving we never get here
        }

        let! first = items |> TaskSeq.drop 2 |> TaskSeq.toArrayAsync
        let! repeat = items |> TaskSeq.drop 2 |> TaskSeq.toArrayAsync

        first |> should equal Array.empty<int>
        repeat |> should equal Array.empty<int>
        x |> should equal 44 // expect: side-effect at end is executed twice by now
    }
