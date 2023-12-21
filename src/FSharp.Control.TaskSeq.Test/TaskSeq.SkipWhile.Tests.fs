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

[<AutoOpen>]
module With =
    /// The only real difference in semantics between the base and the *Inclusive variant lies in whether the final item is returned.
    /// NOTE the semantics are very clear on only propagating a single failing item in the inclusive case.
    let getFunction inclusive isAsync =
        match inclusive, isAsync with
        | false, false -> TaskSeq.skipWhile
        | false, true -> fun pred -> TaskSeq.skipWhileAsync (pred >> Task.fromResult)
        | true, false -> TaskSeq.skipWhileInclusive
        | true, true -> fun pred -> TaskSeq.skipWhileInclusiveAsync (pred >> Task.fromResult)

    /// This is the base condition as one would expect in actual code
    let inline cond x = x <> 6

    /// For each of the tests below, we add a guard that will trigger if the predicate is passed items known to be beyond the
    /// first failing item in the known sequence (which is 1..10)
    let inline condWithGuard x =
        let res = cond x

        if x > 6 then
            failwith "Test sequence should not be enumerated beyond the first item failing the predicate"

        res

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
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhile ((>) 5) // skip while less than 5
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
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusive ((>) 5)
            |> verifyDigitsAsString "GHIJ" // last 4

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusiveAsync (fun x -> task { return x < 5 })
            |> verifyDigitsAsString "GHIJ"
    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-skipWhileInclusive+A returns the empty sequence if always true`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusive ((<) -1)
            |> verifyEmpty

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.skipWhileInclusiveAsync (fun x -> task { return true })
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
    let ``TaskSeq-skipWhile filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.skipWhile condWithGuard
        |> verifyDigitsAsString "ABCDE"

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-skipWhileAsync filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.skipWhileAsync (fun x -> task { return condWithGuard x })
        |> verifyDigitsAsString "ABCDE"

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-skipWhileXXX prove it does not read beyond the failing yield`` (inclusive, isAsync) = task {
        let mutable x = 42 // for this test, the potential mutation should not actually occur
        let functionToTest = getFunction inclusive isAsync ((=) 42)

        let items = taskSeq {
            yield x // Always passes the test; always returned
            yield x * 2 // the failing item (which will also be yielded in the result when using *Inclusive)
            x <- x + 1 // we are proving we never get here
        }

        let expected = if inclusive then [| 42; 84 |] else [| 42 |]

        let! first = items |> functionToTest |> TaskSeq.toArrayAsync
        let! repeat = items |> functionToTest |> TaskSeq.toArrayAsync

        first |> should equal expected
        repeat |> should equal expected
        x |> should equal 42
    }

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-skipWhileXXX prove side effects are executed`` (inclusive, isAsync) = task {
        let mutable x = 41
        let functionToTest = getFunction inclusive isAsync ((>) 50)

        let items = taskSeq {
            x <- x + 1
            yield x
            x <- x + 2
            yield x * 2
            x <- x + 200 // as previously proven, we should not trigger this
        }

        let expectedFirst = if inclusive then [| 42; 44 * 2 |] else [| 42 |]
        let expectedRepeat = if inclusive then [| 45; 47 * 2 |] else [| 45 |]

        let! first = items |> functionToTest |> TaskSeq.toArrayAsync
        x |> should equal 44
        let! repeat = items |> functionToTest |> TaskSeq.toArrayAsync
        x |> should equal 47

        first |> should equal expectedFirst
        repeat |> should equal expectedRepeat
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-skipWhile consumes the prefix of a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first =
            TaskSeq.skipWhile (fun x -> x < 5) ts
            |> TaskSeq.toArrayAsync

        let expected = [| 1..4 |]
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
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

        let expected = [| 1..5 |]
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
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
    let ``TaskSeq-skipWhileXXX exclude all items after predicate fails`` (inclusive, isAsync) =
        let functionToTest = With.getFunction inclusive isAsync

        [ 1; 2; 2; 3; 3; 2; 1 ]
        |> TaskSeq.ofSeq
        |> functionToTest (fun x -> x <= 2)
        |> verifyDigitsAsString (if inclusive then "ABBC" else "ABB")

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-skipWhileXXX stops consuming after predicate fails`` (inclusive, isAsync) =
        let functionToTest = With.getFunction inclusive isAsync

        seq {
            yield! [ 1; 2; 2; 3; 3 ]
            yield failwith "Too far"
        }
        |> TaskSeq.ofSeq
        |> functionToTest (fun x -> x <= 2)
        |> verifyDigitsAsString (if inclusive then "ABBC" else "ABB")
