module TaskSeq.Tests.TakeUntil

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.takeUntil
// TaskSeq.takeUntilAsync
// TaskSeq.takeUntilInclusive
// TaskSeq.takeUntilInclusiveAsync
//

[<AutoOpen>]
module With =
    /// The only real difference in semantics between the base and the *Inclusive variant lies in whether the final item is returned.
    /// NOTE the semantics are very clear on only propagating a single failing item in the inclusive case.
    let getFunction inclusive isAsync =
        match inclusive, isAsync with
        | false, false -> TaskSeq.takeUntil
        | false, true -> fun pred -> TaskSeq.takeUntilAsync (pred >> Task.fromResult)
        | true, false -> TaskSeq.takeUntilInclusive
        | true, true -> fun pred -> TaskSeq.takeUntilInclusiveAsync (pred >> Task.fromResult)

    /// adds '@' to each number and concatenates the chars before calling 'should equal'
    let verifyAsString expected =
        TaskSeq.map char
        >> TaskSeq.map ((+) '@')
        >> TaskSeq.toArrayAsync
        >> Task.map (String >> should equal expected)

    /// This is the base condition as one would expect in actual code
    let inline cond x = x = 6

    /// For each of the tests below, we add a guard that will trigger if the predicate is passed items known to be beyond the
    /// first failing item in the known sequence (which is 1..6)
    let inline condWithGuard x =
        let res = cond x

        if x > 6 then
            failwith "Test sequence should not be enumerated beyond the first item failing the predicate"

        res

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-takeUntil has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.takeUntil ((=) 12)
            |> verifyEmpty

        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.takeUntilAsync ((=) 12 >> Task.fromResult)
            |> verifyEmpty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-takeUntilInclusive has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.takeUntilInclusive ((=) 12)
            |> verifyEmpty

        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.takeUntilInclusiveAsync ((=) 12 >> Task.fromResult)
            |> verifyEmpty
    }

module Immutable =

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeUntil filters correctly`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeUntil condWithGuard
            |> verifyAsString "ABCDE"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeUntilAsync (fun x -> task { return condWithGuard x })
            |> verifyAsString "ABCDE"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeUntil does not pick first item when true`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeUntil ((<>) 0)
            |> verifyAsString ""

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeUntilAsync ((<>) 0 >> Task.fromResult)
            |> verifyAsString ""
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeUntilInclusive filters correctly`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeUntilInclusive condWithGuard
            |> verifyAsString "ABCDEF"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeUntilInclusiveAsync (fun x -> task { return condWithGuard x })
            |> verifyAsString "ABCDEF"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeUntilInclusive always picks at least the first item`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeUntilInclusive ((<>) 0)
            |> verifyAsString "A"

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.takeUntilInclusiveAsync ((<>) 0 >> Task.fromResult)
            |> verifyAsString "A"
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeUntil filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.takeUntil condWithGuard
        |> verifyAsString "ABCDE"

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeUntilAsync filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.takeUntilAsync (fun x -> task { return condWithGuard x })
        |> verifyAsString "ABCDE"

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-takeUntilXXX prove it does not read beyond the failing yield`` (inclusive, isAsync) = task {
        let mutable x = 42 // for this test, the potential mutation should not actually occur
        let functionToTest = getFunction inclusive isAsync ((<>) 42)

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
    let ``TaskSeq-takeUntilXXX prove side effects are executed`` (inclusive, isAsync) = task {
        let mutable x = 41
        let functionToTest = getFunction inclusive isAsync ((<=) 50)

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
    let ``TaskSeq-takeUntil consumes the prefix of a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first =
            TaskSeq.takeUntil (fun x -> x >= 5) ts
            |> TaskSeq.toArrayAsync

        let expected = [| 1..4 |]
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
        let! repeat =
            TaskSeq.takeUntil (fun x -> x < 5) ts
            |> TaskSeq.toArrayAsync

        repeat |> should not' (equal expected)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeUntilInclusiveAsync consumes the prefix for a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first =
            TaskSeq.takeUntilInclusiveAsync (fun x -> task { return x = 6 }) ts
            |> TaskSeq.toArrayAsync

        let expected = [| 1..6 |] // the '6' is included, we are testing "Inclusive"
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
        let! repeat =
            TaskSeq.takeUntilInclusiveAsync (fun x -> task { return x < 5 }) ts
            |> TaskSeq.toArrayAsync

        repeat |> should not' (equal expected)
    }

module Other =
    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-takeUntilXXX exclude all items after predicate fails`` (inclusive, isAsync) =
        let functionToTest = With.getFunction inclusive isAsync

        [ 1; 2; 2; 3; 3; 2; 1 ]
        |> TaskSeq.ofSeq
        |> functionToTest (fun x -> x > 2)
        |> verifyAsString (if inclusive then "ABBC" else "ABB")

    [<Theory>]
    [<InlineData(false, false)>]
    [<InlineData(false, true)>]
    [<InlineData(true, false)>]
    [<InlineData(true, true)>]
    let ``TaskSeq-takeUntilXXX stops consuming after predicate fails`` (inclusive, isAsync) =
        let functionToTest = With.getFunction inclusive isAsync

        seq {
            yield! [ 1; 2; 2; 3; 3 ]
            yield failwith "Too far"
        }
        |> TaskSeq.ofSeq
        |> functionToTest (fun x -> x > 2)
        |> verifyAsString (if inclusive then "ABBC" else "ABB")
