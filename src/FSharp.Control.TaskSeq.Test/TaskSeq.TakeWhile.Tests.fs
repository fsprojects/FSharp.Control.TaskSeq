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
