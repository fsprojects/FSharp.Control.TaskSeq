module TaskSeq.Tests.skipWhile

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.skipWhile
// TaskSeq.skipWhileAsync
// TaskSeq.skipWhileInclusive
// TaskSeq.skipWhileInclusiveAsync
//

exception SideEffectPastEnd of string

[<AutoOpen>]
module With =
    /// The only real difference in semantics between the base and the *Inclusive variant lies in whether the final item is skipped.
    let getFunction inclusive isAsync =
        match inclusive, isAsync with
        | false, false -> TaskSeq.skipWhile
        | false, true -> fun pred -> TaskSeq.skipWhileAsync (pred >> Task.fromResult)
        | true, false -> TaskSeq.skipWhileInclusive
        | true, true -> fun pred -> TaskSeq.skipWhileInclusiveAsync (pred >> Task.fromResult)

module EmptySeq =

    // TaskSeq-skipWhile+A stands for:
    // skipWhile + skipWhileAsync etc.

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-skipWhile+A has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.skipWhile ((=) 12)
            |> verifyEmpty

        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.skipWhileAsync ((=) 12 >> Task.fromResult)
            |> verifyEmpty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-skipWhileInclusive+A has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.skipWhileInclusive ((=) 12)
            |> verifyEmpty

        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.skipWhileInclusiveAsync ((=) 12 >> Task.fromResult)
            |> verifyEmpty
    }

module Immutable =

    // TaskSeq-skipWhile+A stands for:
    // skipWhile + skipWhileAsync etc.

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-skipWhile+A filters correctly`` variant = task {
        // truth table for f(x) = x < 5
        // 1 2 3 4 5 6 7 8 9 10
        // T T T T F F F F F F (stops at first F)
        // x x x x _ _ _ _ _ _ (skips exclusive)
        // A B C D E F G H I J

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhile (fun x -> x < 5)
            |> verifyDigitsAsString "EFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileAsync (fun x -> task { return x < 5 })
            |> verifyDigitsAsString "EFGHIJ"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-skipWhile+A does not skip first item when false`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhile ((=) 0)
            |> verifyDigitsAsString "ABCDEFGHIJ" // all 10 remain!

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileAsync ((=) 0 >> Task.fromResult)
            |> verifyDigitsAsString "ABCDEFGHIJ" // all 10 remain!
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-skipWhileInclusive+A filters correctly`` variant = task {
        // truth table for f(x) = x < 5
        // 1 2 3 4 5 6 7 8 9 10
        // T T T T F F F F F F (stops at first F)
        // x x x x x _ _ _ _ _ (skips inclusively)
        // A B C D E F G H I J

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusive (fun x -> x < 5)
            |> verifyDigitsAsString "FGHIJ" // last 4

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusiveAsync (fun x -> task { return x < 5 })
            |> verifyDigitsAsString "FGHIJ"
    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-skipWhileInclusive+A returns the empty sequence if always true`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusive (fun x -> x > -1) // always true
            |> verifyEmpty

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusiveAsync (fun x -> Task.fromResult (x > -1))
            |> verifyEmpty
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-skipWhileInclusive+A always skips at least the first item`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusive ((=) 0)
            |> verifyDigitsAsString "BCDEFGHIJ"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusiveAsync ((=) 0 >> Task.fromResult)
            |> verifyDigitsAsString "BCDEFGHIJ"
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-skipWhile+A filters correctly`` variant = task {
        // truth table for f(x) = x < 6
        // 1 2 3 4 5 6 7 8 9 10
        // T T T T T F F F F F (stops at first F)
        // x x x x x _ _ _ _ _ (skips exclusively)
        // A B C D E F G H I J

        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.skipWhile (fun x -> x < 6)
            |> verifyDigitsAsString "FGHIJ"

        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.skipWhileAsync (fun x -> task { return x < 6 })
            |> verifyDigitsAsString "FGHIJ"
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-skipWhileInclusive+A filters correctly`` variant = task {
        // truth table for f(x) = x < 6
        // 1 2 3 4 5 6 7 8 9 10
        // T T T T T F F F F F (stops at first F)
        // x x x x x x _ _ _ _ (skips inclusively)
        // A B C D E F G H I J

        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.skipWhileInclusive (fun x -> x < 6)
            |> verifyDigitsAsString "GHIJ"

        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.skipWhileInclusiveAsync (fun x -> task { return x < 6 })
            |> verifyDigitsAsString "GHIJ"
    }

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-skipWhileXXX prove it reads the entire input stream`` (inclusive, isAsync) = task {
        let mutable x = 42 // for this test, the potential mutation should not actually occur
        let functionToTest = getFunction inclusive isAsync ((=) 42)

        let items = taskSeq {
            yield x // Always passes the test; always skipped
            yield x * 2 // Fails the test, skipped depending on "inclusive"
            x <- x + 1 // we are proving we ALWAYS get here
        }

        x |> should equal 42
        let! first = items |> functionToTest |> TaskSeq.toArrayAsync
        x |> should equal 43
        first |> should equal (if inclusive then [||] else [| 84 |])

        let! repeat = items |> functionToTest |> TaskSeq.toArrayAsync
        x |> should equal 44

        repeat
        |> should equal (if inclusive then [| 86 |] else [| 43; 86 |])
    }

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-skipWhileXXX prove side effects are properly executed`` (inclusive, isAsync) = task {
        let mutable x = 41
        let functionToTest = getFunction inclusive isAsync (fun x -> x < 50)

        let items = taskSeq {
            x <- x + 1
            yield x
            x <- x + 2
            yield x * 2
            x <- x + 200 // as previously proven, we should ALWAYS trigger this
        }

        let expectedFirst = if inclusive then [||] else [| 88 |]
        let expectedRepeat = if inclusive then [| 494 |] else [| 245; 494 |]

        x |> should equal 41
        let! first = items |> functionToTest |> TaskSeq.toArrayAsync
        x |> should equal 244

        let! repeat = items |> functionToTest |> TaskSeq.toArrayAsync
        x |> should equal 447

        first |> should equal expectedFirst
        repeat |> should equal expectedRepeat
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-skipWhile consumes the prefix of a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first =
            TaskSeq.skipWhile (fun x -> x < 5) ts
            |> TaskSeq.toArrayAsync

        let expected = [| 5..10 |]
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
        // which means the original sequence has now changed due to the side effect
        let! repeat =
            TaskSeq.skipWhile (fun x -> x < 5) ts
            |> TaskSeq.toArrayAsync

        repeat |> should not' (equal expected)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-skipWhileInclusiveAsync consumes the prefix for a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first =
            TaskSeq.skipWhileInclusiveAsync (fun x -> task { return x < 5 }) ts
            |> TaskSeq.toArrayAsync

        let expected = [| 6..10 |]
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
        // which means the original sequence has now changed due to the side effect
        let! repeat =
            TaskSeq.skipWhileInclusiveAsync (fun x -> task { return x < 5 }) ts
            |> TaskSeq.toArrayAsync

        repeat |> should not' (equal expected)
    }

module Other =
    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-skipWhileXXX should include all items after predicate fails`` (inclusive, isAsync) = task {
        do!
            [ 1; 2; 2; 3; 3; 2; 1 ]
            |> TaskSeq.ofSeq
            |> TaskSeq.skipWhile (fun x -> x <= 2)
            |> verifyDigitsAsString "CCBA"

        do!
            [ 1; 2; 2; 3; 3; 2; 1 ]
            |> TaskSeq.ofSeq
            |> TaskSeq.skipWhileInclusive (fun x -> x <= 2)
            |> verifyDigitsAsString "CBA"

        do!
            [ 1; 2; 2; 3; 3; 2; 1 ]
            |> TaskSeq.ofSeq
            |> TaskSeq.skipWhileAsync (fun x -> Task.fromResult (x <= 2))
            |> verifyDigitsAsString "CCBA"

        do!
            [ 1; 2; 2; 3; 3; 2; 1 ]
            |> TaskSeq.ofSeq
            |> TaskSeq.skipWhileInclusiveAsync (fun x -> Task.fromResult (x <= 2))
            |> verifyDigitsAsString "CBA"
    }

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-skipWhileXXX stops consuming after predicate fails`` (inclusive, isAsync) =
        let testSkipper skipper =
            fun () ->
                seq {
                    yield! [ 1; 2; 2; 3; 3 ]
                    yield SideEffectPastEnd "Too far" |> raise
                }
                |> TaskSeq.ofSeq
                |> skipper
                |> consumeTaskSeq
            |> should throwAsyncExact typeof<SideEffectPastEnd>

        testSkipper (TaskSeq.skipWhile (fun x -> x <= 2))
        testSkipper (TaskSeq.skipWhileInclusive (fun x -> x <= 2))
        testSkipper (TaskSeq.skipWhileAsync (fun x -> Task.fromResult (x <= 2)))
        testSkipper (TaskSeq.skipWhileInclusiveAsync (fun x -> Task.fromResult (x <= 2)))
