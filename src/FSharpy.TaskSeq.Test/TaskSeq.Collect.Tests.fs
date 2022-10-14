namespace FSharpy.Tests

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

type Collect(output) =

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-collect operates in correct order`` () =
        logStart output

        task {
            let! sq =
                createDummyTaskSeq 10
                |> TaskSeq.collect (fun item -> taskSeq {
                    yield char (item + 64)
                    yield char (item + 65)
                })
                |> TaskSeq.toSeqCachedAsync

            sq
            |> Seq.map string
            |> String.concat ""
            |> should equal "ABBCCDDEEFFGGHHIIJJK"
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-collectSeq operates in correct order`` () =
        logStart output

        task {
            let! sq =
                createDummyTaskSeq 10
                |> TaskSeq.collectSeq (fun item -> seq {
                    yield char (item + 64)
                    yield char (item + 65)
                })
                |> TaskSeq.toSeqCachedAsync

            sq
            |> Seq.map string
            |> String.concat ""
            |> should equal "ABBCCDDEEFFGGHHIIJJK"
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-collect with empty task sequences`` () =
        logStart output

        task {
            let! sq =
                createDummyTaskSeq 10
                |> TaskSeq.collect (fun _ -> TaskSeq.ofSeq Seq.empty)
                |> TaskSeq.toSeqCachedAsync

            Seq.isEmpty sq |> should be True
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-collectSeq with empty sequences`` () =
        logStart output

        task {
            let! sq =
                createDummyTaskSeq 10
                |> TaskSeq.collectSeq (fun _ -> Seq.empty<int>)
                |> TaskSeq.toSeqCachedAsync

            Seq.isEmpty sq |> should be True
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-empty is empty`` () =
        logStart output

        task {
            let! sq = TaskSeq.empty<string> |> TaskSeq.toSeqCachedAsync
            Seq.isEmpty sq |> should be True
        }
