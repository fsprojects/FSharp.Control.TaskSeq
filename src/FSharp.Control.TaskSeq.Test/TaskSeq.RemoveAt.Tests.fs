module TaskSeq.Tests.RemoveAt

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control


//
// TaskSeq.removeAt
// TaskSeq.removeManyAt
//

exception SideEffectPastEnd of string

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-removeAt(0) on empty input raises`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.removeAt 0
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-removeManyAt(0) on empty input raises`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.removeManyAt 0 0
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-removeAt(-1) on empty input should throw ArgumentException without consuming`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.removeAt -1
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () -> Gen.getEmptyVariant variant |> TaskSeq.removeAt -1 |> ignore // task is not awaited

        |> should throw typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-removeManyAt(-1) on empty input should throw ArgumentException without consuming`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.removeManyAt -1 0
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.removeManyAt -1 0
            |> ignore

        |> should throw typeof<ArgumentException>

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeAt can remove last item`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeAt 9
            |> verifyDigitsAsString "ABCDEFGHI"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeAt removes the item at indexed positions`` variant = task {

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeAt 0
            |> verifyDigitsAsString "BCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeAt 1
            |> verifyDigitsAsString "ACDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeAt 5
            |> verifyDigitsAsString "ABCDEGHIJ"

    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeAt can be repeated in a chain`` variant = task {

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeAt 0
            |> TaskSeq.removeAt 0
            |> TaskSeq.removeAt 0
            |> TaskSeq.removeAt 0
            |> TaskSeq.removeAt 0
            |> verifyDigitsAsString "FGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeAt 9
            |> TaskSeq.removeAt 8
            |> TaskSeq.removeAt 7
            |> TaskSeq.removeAt 6
            |> TaskSeq.removeAt 5 // sequence gets shorter, pick last
            |> verifyDigitsAsString "ABCDE"
    }

    [<Fact>]
    let ``TaskSeq-removeAt can be applied to an infinite task sequence`` () =
        TaskSeq.initInfinite id
        |> TaskSeq.removeAt 10_000
        |> TaskSeq.item 10_000
        |> Task.map (should equal 10_001)


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeAt throws when there are not enough elements`` variant =
        fun () ->
            TaskSeq.singleton 1
            // remove after 1
            |> TaskSeq.removeAt 2
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.removeAt 10
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.removeAt 10_000_000
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeManyAt can remove last item`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 9 1
            |> verifyDigitsAsString "ABCDEFGHI"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeManyAt can remove multiple items`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 1 5
            |> verifyDigitsAsString "AGHIJ"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeManyAt can with a large count past the end of the sequence is fine`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 2 20_000 // try to remove too many is fine, like Seq.removeManyAt (regardless the docs at time of writing)
            |> verifyDigitsAsString "AB"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeManyAt does not remove any item when count is zero`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 9 0
            |> verifyDigitsAsString "ABCDEFGHIJ"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeManyAt does not remove any item when count is negative`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 1 -99
            |> verifyDigitsAsString "ABCDEFGHIJ"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeManyAt removes items at indexed positions`` variant = task {

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 0 5
            |> verifyDigitsAsString "FGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 1 3
            |> verifyDigitsAsString "AEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 5 5
            |> verifyDigitsAsString "ABCDE"

    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeManyAt can be repeated in a chain`` variant = task {

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 0 1
            |> TaskSeq.removeManyAt 0 2
            |> TaskSeq.removeManyAt 0 3
            |> verifyDigitsAsString "GHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 9 1 // pick last, result ABCDEFGHIJ
            |> TaskSeq.removeManyAt 6 1 // then from 6th pos, result ABCDEFHI
            |> TaskSeq.removeManyAt 3 2 // from 3rd pos take 2, result ABCFHI
            |> TaskSeq.removeManyAt 0 2 // from start, take 2, result CFHI
            |> verifyDigitsAsString "CFHI"
    }

    [<Fact>]
    let ``TaskSeq-removeManyAt can be applied to an infinite task sequence`` () =
        TaskSeq.initInfinite id
        |> TaskSeq.removeManyAt 10_000 5_000
        |> TaskSeq.item 12_000
        |> Task.map (should equal 17_000)


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-removeManyAt throws when there are not enough elements for index`` variant =
        // NOTE: only raises if INDEX is out of bounds, not when COUNT is out of bounds!!!

        fun () ->
            TaskSeq.singleton 1
            // remove after 1
            |> TaskSeq.removeManyAt 2 0 // regardless of count, it should raise
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 10 5
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.removeManyAt 10_000_000 -5 // even with neg. count
            |> consumeTaskSeq

        |> should throwAsyncExact typeof<ArgumentException>



module SideEffects =

    // NOTES:
    //
    // no tests, it is not possible to create a meaningful side-effect test, as any consuming after
    // removing an item would logically require the side effect to be executed the normal way

    // PoC test
    [<Fact>]
    let ``Seq-removeAt (poc-proof) will execute side effect before index`` () =
        // NOTE: this test is for documentation purposes only, to show this behavior that is tested in this module
        // this shows that Seq.removeAt executes more side effects than necessary.

        let mutable x = 42

        let items = seq {
            yield x
            x <- x + 1 // we are proving this gets executed with removeAt(0), BUT this is the result of Seq.item
            yield x * 2
        }

        items
        |> Seq.removeAt 0
        |> Seq.item 0 // consume anything (this is why there's nothing to test with Seq.removeAt, as this is always true after removing an item)
        |> ignore

        x |> should equal 43 // one time side effect executed. QED
