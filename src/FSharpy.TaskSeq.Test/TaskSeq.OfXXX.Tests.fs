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

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-ofAsyncArray should succeed`` () =
        logStart output

        Array.init 10 (fun x -> async { return x })
        |> TaskSeq.ofAsyncArray
        |> validateSequence

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-ofAsyncList should succeed`` () =
        logStart output

        List.init 10 (fun x -> async { return x })
        |> TaskSeq.ofAsyncList
        |> validateSequence

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-ofAsyncSeq should succeed`` () =
        logStart output

        Seq.init 10 (fun x -> async { return x })
        |> TaskSeq.ofAsyncSeq
        |> validateSequence

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-ofTaskArray should succeed`` () =
        logStart output

        Array.init 10 (fun x -> task { return x })
        |> TaskSeq.ofTaskArray
        |> validateSequence

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-ofTaskList should succeed`` () =
        logStart output

        List.init 10 (fun x -> task { return x })
        |> TaskSeq.ofTaskList
        |> validateSequence

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-ofTaskSeq should succeed`` () =
        logStart output

        Seq.init 10 (fun x -> task { return x })
        |> TaskSeq.ofTaskSeq
        |> validateSequence

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-ofArray should succeed`` () =
        logStart output
        Array.init 10 id |> TaskSeq.ofArray |> validateSequence

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-ofList should succeed`` () =
        logStart output
        List.init 10 id |> TaskSeq.ofList |> validateSequence

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-ofSeq should succeed`` () =
        logStart output
        Seq.init 10 id |> TaskSeq.ofSeq |> validateSequence
