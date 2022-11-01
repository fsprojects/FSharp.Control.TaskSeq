module FSharpy.Tests.Filter

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-filter has no effect`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.filter ((=) 12)
        |> TaskSeq.toListAsync
        |> Task.map (List.isEmpty >> should be True)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-filterAsync has no effect`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.filterAsync (fun x -> task { return x = 12 })
        |> TaskSeq.toListAsync
        |> Task.map (List.isEmpty >> should be True)

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filter filters correctly`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.filter ((<=) 5) // greater than
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "EFGHIJ")

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filterAsync filters correctly`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.filterAsync (fun x -> task { return x <= 5 })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")

module SideEffects =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filter filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.filter ((<=) 5) // greater than
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "EFGHIJ")

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-filterAsync filters correctly`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.filterAsync (fun x -> task { return x <= 5 })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync
        |> Task.map (String >> should equal "ABCDE")
