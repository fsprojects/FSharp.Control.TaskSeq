namespace FSharpy.Tests

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

type Map(output) =

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-map maps in correct order`` () =
        logStart output

        task {
            let! sq =
                createDummyTaskSeq 10
                |> TaskSeq.map (fun item -> char (item + 64))
                |> TaskSeq.toSeqCachedAsync

            sq
            |> Seq.map string
            |> String.concat ""
            |> should equal "ABCDEFGHIJ"
        }

    [<Fact(Skip = "CI test runner chokes!")>]
    let ``TaskSeq-mapAsync maps in correct order`` () =
        logStart output

        task {
            let! sq =
                createDummyTaskSeq 10
                |> TaskSeq.mapAsync (fun item -> task { return char (item + 64) })
                |> TaskSeq.toSeqCachedAsync

            sq
            |> Seq.map string
            |> String.concat ""
            |> should equal "ABCDEFGHIJ"
        }
