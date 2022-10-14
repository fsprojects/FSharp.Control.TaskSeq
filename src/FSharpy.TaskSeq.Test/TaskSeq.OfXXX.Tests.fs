module FSharpy.Tests.``Conversion-From``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

let validateSequence sq = task {
    let! sq = TaskSeq.toArrayAsync sq
    do sq |> Seq.toArray |> should equal [| 0..9 |]
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-ofAsyncArray should succeed`` () =
    Array.init 10 (fun x -> async { return x })
    |> TaskSeq.ofAsyncArray
    |> validateSequence

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-ofAsyncList should succeed`` () =
    List.init 10 (fun x -> async { return x })
    |> TaskSeq.ofAsyncList
    |> validateSequence

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-ofAsyncSeq should succeed`` () =
    Seq.init 10 (fun x -> async { return x })
    |> TaskSeq.ofAsyncSeq
    |> validateSequence

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-ofTaskArray should succeed`` () =
    Array.init 10 (fun x -> task { return x })
    |> TaskSeq.ofTaskArray
    |> validateSequence

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-ofTaskList should succeed`` () =
    List.init 10 (fun x -> task { return x })
    |> TaskSeq.ofTaskList
    |> validateSequence

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-ofTaskSeq should succeed`` () =
    Seq.init 10 (fun x -> task { return x })
    |> TaskSeq.ofTaskSeq
    |> validateSequence

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-ofArray should succeed`` () = Array.init 10 id |> TaskSeq.ofArray |> validateSequence

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-ofList should succeed`` () = List.init 10 id |> TaskSeq.ofList |> validateSequence

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-ofSeq should succeed`` () = Seq.init 10 id |> TaskSeq.ofSeq |> validateSequence
