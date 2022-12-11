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

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeWhile filters correctly`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.takeWhile (fun x -> x <= 5)
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-takeWhileAsync filters correctly`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.takeWhileAsync (fun x -> task { return x <= 5 })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhile filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.takeWhile (fun x -> x <= 5)
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-takeWhileAsync filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.takeWhileAsync (fun x -> task { return x <= 5 })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")
