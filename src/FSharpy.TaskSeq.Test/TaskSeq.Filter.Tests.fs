module FSharpy.Tests.Filter

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

[<Fact>]
let ``TaskSeq-filter on an empty sequence`` () = task {
    let! empty =
        TaskSeq.empty
        |> TaskSeq.filter ((=) 12)
        |> TaskSeq.toListAsync

    List.isEmpty empty |> should be True
}

[<Fact>]
let ``TaskSeq-filterAsync on an empty sequence`` () = task {
    let! empty =
        TaskSeq.empty
        |> TaskSeq.filterAsync (fun x -> task { return x = 12 })
        |> TaskSeq.toListAsync

    List.isEmpty empty |> should be True
}

[<Fact>]
let ``TaskSeq-filter filters correctly`` () = task {
    let! alphabet =
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
        |> TaskSeq.filter ((<=) 26) // lambda of '>' etc inverts order of args, so this means 'greater than'
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync

    // we filtered all digits above-or-equal-to 26
    String alphabet |> should equal "Z[\]^_`abcdefghijklmnopqr"
}

[<Fact>]
let ``TaskSeq-filterAsync filters correctly`` () = task {
    let! alphabet =
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
        |> TaskSeq.filterAsync (fun x -> task { return x <= 26 })
        |> TaskSeq.map char
        |> TaskSeq.map ((+) '@')
        |> TaskSeq.toArrayAsync

    String alphabet |> should equal "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
}
