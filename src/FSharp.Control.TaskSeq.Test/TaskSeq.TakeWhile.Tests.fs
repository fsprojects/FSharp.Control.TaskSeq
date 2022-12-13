module TaskSeq.Tests.TakeWhile

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.takeWhile
// TaskSeq.takeWhileAsync
// TaskSeq.takeWhileInclusive
// TaskSeq.takeWhileInclusiveAsync
//

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-takeWhile has no effect`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.takeWhile ((=) 12)
        |> TaskSeq.toListAsync
        |> Task.map (List.isEmpty >> should be True)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-takeWhileAsync has no effect`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.takeWhileAsync (fun x -> task { return x = 12 })
        |> TaskSeq.toListAsync
        |> Task.map (List.isEmpty >> should be True)

// The primary requirement is that items after the item failing the predicate must be excluded
module FiltersAfterFail =
    [<Theory; InlineData false; InlineData true>]
    let ``TaskSeq-takeWhile(Inclusive)? excludes all items after predicate fails`` inclusive =
        // The only real difference in semantics between the base and the *Inclusive variant lies in whether the final item is returned
        // NOTE the semantics are very clear on only propagating a single failing item in the inclusive case
        let f, expected =
            if inclusive then TaskSeq.takeWhileInclusive, "ABBC"
            else TaskSeq.takeWhile, "ABB"
        seq { 1; 2; 2; 3; 3; 2; 1 }
        |> TaskSeq.ofSeq
        |> f (fun x -> x <= 2)
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal expected)

    // Same as preceding test, just with Async functions
    [<Theory; InlineData false; InlineData true>]
    let ``TaskSeq-takeWhile(Inclusive)?Async excludes all items after after predicate fails`` inclusive =
        let f, expected =
            if inclusive then TaskSeq.takeWhileInclusiveAsync, "ABBC"
            else TaskSeq.takeWhileAsync, "ABB"
        taskSeq { 1; 2; 2; 3; 3; 2; 1 }
        |> f (fun x -> task { return x <= 2 })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal expected)

// Covers the fact that it's not sufficient to merely exclude successor items - it's also critical that the enumeration terminates
module StopsEnumeratingAfterFail =
    [<Theory; InlineData false; InlineData true>]
    let ``TaskSeq-takeWhile(Inclusive)? stops consuming after predicate fails`` inclusive =
        let f, expected =
            if inclusive then TaskSeq.takeWhileInclusive, "ABBC"
            else TaskSeq.takeWhile, "ABB"
        seq { 1; 2; 2; 3; 3; failwith "Too far" }
        |> TaskSeq.ofSeq
        |> f (fun x -> x <= 2)
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal expected)

    [<Theory; InlineData false; InlineData true>]
    let ``TaskSeq-takeWhile(Inclusive)?Async stops consuming after predicate fails`` inclusive =
        let f, expected =
            if inclusive then TaskSeq.takeWhileInclusiveAsync, "ABBC"
            else TaskSeq.takeWhileAsync, "ABB"
        taskSeq { 1; 2; 2; 3; 3; failwith "Too far" }
        |> f (fun x -> task { return x <= 2 })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal expected)

/// This is the base condition as one would expect in actual code
let inline cond x = x <> 6

/// For each of the tests below, we add a guard that will trigger if the predicate is passed items known to be beyond the
/// first failing item in the known sequence (which is 1..10)
let inline condWithGuard x =
    let res = cond x
    if x > 6 then failwith "Test sequence should not be enumerated beyond the first item failing the predicate"
    res

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeWhile filters correctly`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.takeWhile condWithGuard
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeWhileAsync filters correctly`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.takeWhileAsync (fun x -> task { return condWithGuard x })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhile filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.takeWhile condWithGuard
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhileAsync filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.takeWhileAsync (fun x -> task { return condWithGuard x })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")

    [<Theory; InlineData(false, false); InlineData(true, false); InlineData(false, true); InlineData(true, true)>]
    let ``TaskSeq-takeWhile(Inclusive)?(Async)? __special-case__ prove it does not read beyond the failing yield`` (inclusive, async) = task {
        let mutable x = 42 // for this test, the potential mutation should not actually occur

        let items = taskSeq {
            yield x // Always passes the test; always returned
            yield x * 2 // the failing item (which will also be yielded in the result when using *Inclusive)
            x <- x + 1 // we are proving we never get here
        }

        let f =
            match inclusive, async with
            | false, false -> TaskSeq.takeWhile (fun x -> x = 42)
            | true, false -> TaskSeq.takeWhileInclusive (fun x -> x = 42)
            | false, true -> TaskSeq.takeWhileAsync (fun x -> task { return x = 42 })
            | true, true -> TaskSeq.takeWhileInclusiveAsync (fun x -> task { return x = 42 })

        let expected = if inclusive then [| 42; 84 |] else [| 42 |]

        let! first = items |> f |> TaskSeq.toArrayAsync
        let! repeat = items |> f |> TaskSeq.toArrayAsync

        first |> should equal expected
        repeat |> should equal expected
        x |> should equal 42
    }

    [<Theory; InlineData(false, false); InlineData(true, false); InlineData(false, true); InlineData(true, true)>]
    let ``TaskSeq-takeWhile(Inclusive)?(Async)? __special-case__ prove side effects are executed`` (inclusive, async) = task {
        let mutable x = 41

        let items = taskSeq {
            x <- x + 1
            yield x
            x <- x + 2
            yield x * 2
            x <- x + 200 // as previously proven, we should not trigger this
        }

        let f =
            match inclusive, async with
            | false, false -> TaskSeq.takeWhile (fun x -> x < 50)
            | true, false -> TaskSeq.takeWhileInclusive (fun x -> x < 50)
            | false, true -> TaskSeq.takeWhileAsync (fun x -> task { return x < 50 })
            | true, true -> TaskSeq.takeWhileInclusiveAsync (fun x -> task { return x < 50 })

        let expectedFirst = if inclusive then [| 42; 44*2 |] else [| 42 |]
        let expectedRepeat = if inclusive then [| 45; 47*2 |] else [| 45 |]

        let! first = items |> f |> TaskSeq.toArrayAsync
        x |> should equal 44
        let! repeat = items |> f |> TaskSeq.toArrayAsync
        x |> should equal 47

        first |> should equal expectedFirst
        repeat |> should equal expectedRepeat
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhile consumes the prefix of a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first = TaskSeq.takeWhile (fun x -> x < 5) ts |> TaskSeq.toArrayAsync
        let expected = [| 1..4 |]
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
        let! repeat = TaskSeq.takeWhile (fun x -> x < 5) ts |> TaskSeq.toArrayAsync
        repeat |> should not' (equal expected)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhileInclusiveAsync consumes the prefix for a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first = TaskSeq.takeWhileInclusiveAsync (fun x -> task { return x < 5 }) ts |> TaskSeq.toArrayAsync
        let expected = [| 1..5 |]
        first |> should equal expected

        // side effect, reiterating causes it to resume from where we left it (minus the failing item)
        let! repeat = TaskSeq.takeWhileInclusiveAsync (fun x -> task { return x < 5 }) ts |> TaskSeq.toArrayAsync
        repeat |> should not' (equal expected)
    }
