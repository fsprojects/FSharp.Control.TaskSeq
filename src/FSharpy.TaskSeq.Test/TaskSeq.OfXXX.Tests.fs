module FSharpy.TaskSeq.Tests.``Conversion-From``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

let validateSequence sq = task {
    let! sq = TaskSeq.toArrayAsync sq
    do sq |> Seq.toArray |> should equal [| 0..9 |]
}

[<Fact>]
let ``TaskSeq-ofAsyncArray should succeed`` () =
    Array.init 10 (fun x -> async { return x })
    |> TaskSeq.ofAsyncArray
    |> validateSequence

[<Fact>]
let ``TaskSeq-ofAsyncList should succeed`` () =
    List.init 10 (fun x -> async { return x })
    |> TaskSeq.ofAsyncList
    |> validateSequence

[<Fact>]
let ``TaskSeq-ofAsyncSeq should succeed`` () =
    Seq.init 10 (fun x -> async { return x })
    |> TaskSeq.ofAsyncSeq
    |> validateSequence

[<Fact>]
let ``TaskSeq-ofTaskArray should succeed`` () =
    Array.init 10 (fun x -> task { return x })
    |> TaskSeq.ofTaskArray
    |> validateSequence

[<Fact>]
let ``TaskSeq-ofTaskList should succeed`` () =
    List.init 10 (fun x -> task { return x })
    |> TaskSeq.ofTaskList
    |> validateSequence

[<Fact>]
let ``TaskSeq-ofTaskSeq should succeed`` () =
    Seq.init 10 (fun x -> task { return x })
    |> TaskSeq.ofTaskSeq
    |> validateSequence

[<Fact>]
let ``TaskSeq-ofArray should succeed`` () = Array.init 10 id |> TaskSeq.ofArray |> validateSequence

[<Fact>]
let ``TaskSeq-ofList should succeed`` () = List.init 10 id |> TaskSeq.ofList |> validateSequence

[<Fact>]
let ``TaskSeq-ofSeq should succeed`` () = Seq.init 10 id |> TaskSeq.ofSeq |> validateSequence
