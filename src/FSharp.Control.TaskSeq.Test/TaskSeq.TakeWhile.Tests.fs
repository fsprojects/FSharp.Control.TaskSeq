module TaskSeq.Tests.TakeWhile

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.takeWhile
// TaskSeq.takeWhileAsync
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

module Terminates =
    [<Fact>]
    let ``TaskSeq-takeWhile stops after predicate fails`` () =
        seq { 1; 2; 3; failwith "Too far" }
        |> TaskSeq.ofSeq
        |> TaskSeq.takeWhile (fun x -> x <= 2)
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "AB")

    [<Fact>]
    let ``TaskSeq-takeWhileAsync stops after predicate fails`` () =
        taskSeq { 1; 2; 3; failwith "Too far" }
        |> TaskSeq.takeWhileAsync (fun x -> task { return x <= 2 })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "AB")

// This is the base condition as one would expect in actual code
let inline cond x = x <> 6

// For each of the tests below, we add a guard that will trigger if the predicate is passed items known to be beyond the
// first failing item in the known sequence (which is 1..10)
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
