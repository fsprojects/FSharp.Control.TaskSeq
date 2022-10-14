namespace FSharpy.Tests

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

type ``Other functions``(output) =

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-empty returns an empty sequence`` () =
        logStart output

        task {
            let! sq = TaskSeq.empty<string> |> TaskSeq.toSeqCachedAsync
            Seq.isEmpty sq |> should be True
            Seq.length sq |> should equal 0
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-isEmpty returns true for empty`` () =
        logStart output

        task {
            let! isEmpty = TaskSeq.empty<string> |> TaskSeq.isEmpty
            isEmpty |> should be True
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-isEmpty returns false for non-empty`` () =
        logStart output

        task {
            let! isEmpty = taskSeq { yield 42 } |> TaskSeq.isEmpty
            isEmpty |> should be False
        }
