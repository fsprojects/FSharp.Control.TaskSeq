namespace FSharpy.Tests

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

type ``Conversion-From``(output) =

    let validateSequence sq = task {
        let! sq = TaskSeq.toArrayAsync sq
        do sq |> Seq.toArray |> should equal [| 0..9 |]
    }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-ofAsyncArray should succeed`` () =
        logStart output

        Array.init 10 (fun x -> async { return x })
        |> TaskSeq.ofAsyncArray
        |> validateSequence

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-ofAsyncList should succeed`` () =
        logStart output

        List.init 10 (fun x -> async { return x })
        |> TaskSeq.ofAsyncList
        |> validateSequence

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-ofAsyncSeq should succeed`` () =
        logStart output

        Seq.init 10 (fun x -> async { return x })
        |> TaskSeq.ofAsyncSeq
        |> validateSequence

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-ofTaskArray should succeed`` () =
        logStart output

        Array.init 10 (fun x -> task { return x })
        |> TaskSeq.ofTaskArray
        |> validateSequence

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-ofTaskList should succeed`` () =
        logStart output

        List.init 10 (fun x -> task { return x })
        |> TaskSeq.ofTaskList
        |> validateSequence

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-ofTaskSeq should succeed`` () =
        logStart output

        Seq.init 10 (fun x -> task { return x })
        |> TaskSeq.ofTaskSeq
        |> validateSequence

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-ofArray should succeed`` () =
        logStart output
        Array.init 10 id |> TaskSeq.ofArray |> validateSequence

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-ofList should succeed`` () =
        logStart output
        List.init 10 id |> TaskSeq.ofList |> validateSequence

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-ofSeq should succeed`` () =
        logStart output
        Seq.init 10 id |> TaskSeq.ofSeq |> validateSequence
