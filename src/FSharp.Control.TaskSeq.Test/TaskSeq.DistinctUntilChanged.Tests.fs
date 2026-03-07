module TaskSeq.Tests.DistinctUntilChanged

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.distinctUntilChanged
//


module EmptySeq =
    [<Fact>]
    let ``TaskSeq-distinctUntilChanged with null source raises`` () = assertNullArg <| fun () -> TaskSeq.distinctUntilChanged null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-distinctUntilChanged has no effect`` variant = task {
        do!
            Gen.getEmptyVariant variant
            |> TaskSeq.distinctUntilChanged
            |> TaskSeq.toListAsync
            |> Task.map (List.isEmpty >> should be True)
    }

module Functionality =
    [<Fact>]
    let ``TaskSeq-distinctUntilChanged should return no consecutive duplicates`` () = task {
        let ts =
            [ 'A'; 'A'; 'B'; 'Z'; 'C'; 'C'; 'Z'; 'C'; 'D'; 'D'; 'D'; 'Z' ]
            |> TaskSeq.ofList

        let! xs = ts |> TaskSeq.distinctUntilChanged |> TaskSeq.toListAsync

        xs
        |> List.map string
        |> String.concat ""
        |> should equal "ABZCZCDZ"
    }
