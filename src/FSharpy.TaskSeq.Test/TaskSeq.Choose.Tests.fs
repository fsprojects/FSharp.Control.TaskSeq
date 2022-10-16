module FSharpy.Tests.Choose

open System
open System.Threading.Tasks

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

[<Fact>]
let ``TaskSeq-choose on an empty sequence`` () = task {
    let! empty =
        TaskSeq.empty
        |> TaskSeq.choose (fun _ -> Some 42)
        |> TaskSeq.toListAsync

    List.isEmpty empty |> should be True
}

[<Fact>]
let ``TaskSeq-chooseAsync on an empty sequence`` () = task {
    let! empty =
        TaskSeq.empty
        |> TaskSeq.chooseAsync (fun _ -> task { return Some 42 })
        |> TaskSeq.toListAsync

    List.isEmpty empty |> should be True
}

[<Fact>]
let ``TaskSeq-choose can convert and filter`` () = task {
    let! alphabet =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.choose (fun number -> if number <= 26 then Some(char number + '@') else None)
        |> TaskSeq.toArrayAsync

    String alphabet |> should equal "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
}

[<Fact>]
let ``TaskSeq-chooseAsync can convert and filter`` () = task {
    let! alphabet =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.chooseAsync (fun number -> task { return if number <= 26 then Some(char number + '@') else None })
        |> TaskSeq.toArrayAsync

    String alphabet |> should equal "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
}
