module TaskSeq.Tests.TakeWhile

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.takeWhile
// TaskSeq.takeWhileAsync
// TaskSeq.takeWhileInclusive
// TaskSeq.takeWhileInclusiveAsync
//

[<AutoOpen>]
module With =
    /// The only real difference in semantics between the base and the *Inclusive variant lies in whether the final item is returned.
    /// NOTE the semantics are very clear on only propagating a single failing item in the inclusive case.
    let getFunction inclusive isAsync =
        match inclusive, isAsync with
        | false, false -> TaskSeq.takeWhile
        | false, true -> fun pred -> TaskSeq.takeWhileAsync (pred >> Task.fromResult)
        | true, false -> TaskSeq.takeWhileInclusive
        | true, true -> fun pred -> TaskSeq.takeWhileInclusiveAsync (pred >> Task.fromResult)

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

    // TaskSeq-takeWhile+A stands for:
    // takeWhile + takeWhileAsync etc.

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-takeWhile+A has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.takeWhile ((=) 12)
            |> verifyEmpty

        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.takeWhileAsync ((=) 12 >> Task.fromResult)
            |> verifyEmpty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-takeWhileInclusive+A has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.takeWhileInclusive ((=) 12)
            |> verifyEmpty

        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.takeWhileInclusiveAsync ((=) 12 >> Task.fromResult)
            |> verifyEmpty
    }

module Immutable =

    // TaskSeq-takeWhile+A stands for:
    // takeWhile + takeWhileAsync etc.

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeWhile+A filters correctly`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeWhile condWithGuard
            |> verifyDigitsAsString "ABCDE"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeWhileAsync (fun x -> task { return condWithGuard x })
            |> verifyDigitsAsString "ABCDE"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeWhile+A does not pick first item when false`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeWhile ((=) 0)
            |> verifyDigitsAsString ""

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeWhileAsync ((=) 0 >> Task.fromResult)
            |> verifyDigitsAsString ""
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeWhileInclusive+A filters correctly`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeWhileInclusive condWithGuard
            |> verifyDigitsAsString "ABCDEF"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeWhileInclusiveAsync (fun x -> task { return condWithGuard x })
            |> verifyDigitsAsString "ABCDEF"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeWhileInclusive+A always pick at least the first item`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeWhileInclusive ((=) 0)
            |> verifyDigitsAsString "A"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeWhileInclusiveAsync ((=) 0 >> Task.fromResult)
            |> verifyDigitsAsString "A"
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhile+A filters correctly`` variant = task {
        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.takeWhile condWithGuard
            |> verifyDigitsAsString "ABCDE"

        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.takeWhileAsync (fun x -> task { return condWithGuard x })
            |> verifyDigitsAsString "ABCDE"
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhileInclusive+A filters correctly`` variant = task {
        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.takeWhileInclusive condWithGuard
            |> verifyDigitsAsString "ABCDEF"

        do!
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.takeWhileInclusiveAsync (fun x -> task { return condWithGuard x })
            |> verifyDigitsAsString "ABCDEF"
    }

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-takeWhileXXX prove it does not read beyond the failing yield`` (inclusive, isAsync) = task {
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
    let ``TaskSeq-takeWhileXXX prove side effects are executed`` (inclusive, isAsync) = task {
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

        x |> should equal 41
        let! first = items |> functionToTest |> TaskSeq.toArrayAsync
        x |> should equal 44
        let! repeat = items |> functionToTest |> TaskSeq.toArrayAsync
        x |> should equal 47

        first |> should equal expectedFirst
        repeat |> should equal expectedRepeat
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhile consumes the prefix of a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first =
            TaskSeq.takeWhile (fun x -> x < 5) ts
            |> TaskSeq.toArrayAsync

        let expected = [| 1..4 |]
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
        // which means the original sequence has now changed due to the side effect
        let! repeat =
            TaskSeq.takeWhile (fun x -> x < 5) ts
            |> TaskSeq.toArrayAsync

        repeat |> should not' (equal expected)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhileInclusiveAsync consumes the prefix for a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first =
            TaskSeq.takeWhileInclusiveAsync (fun x -> task { return x < 5 }) ts
            |> TaskSeq.toArrayAsync

        let expected = [| 1..5 |]
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
        // which means the original sequence has now changed due to the side effect
        let! repeat =
            TaskSeq.takeWhileInclusiveAsync (fun x -> task { return x < 5 }) ts
            |> TaskSeq.toArrayAsync

        repeat |> should not' (equal expected)
    }

module Other =
    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-takeWhileXXX should exclude all items after predicate fails`` (inclusive, isAsync) =
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
    let ``TaskSeq-takeWhileXXX stops consuming after predicate fails`` (inclusive, isAsync) =
        let functionToTest = With.getFunction inclusive isAsync

        seq {
            yield! [ 1; 2; 2; 3; 3 ]
            yield failwith "Too far"
        }
        |> TaskSeq.ofSeq
        |> functionToTest (fun x -> x <= 2)
        |> verifyDigitsAsString (if inclusive then "ABBC" else "ABB")
