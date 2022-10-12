namespace FSharpy.TaskSeq.Tests

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

module TaskSeqTests =
    let createAsyncEnum count =
        let tasks = DummyTaskFactory().CreateBunchOfTasks count

        taskSeq {
            for task in tasks do
                // cannot use `yield!` here, as `taskSeq` expects it to return a seq
                let! x = task ()
                yield x
        }

    [<Fact>]
    let ``TaskSeq-iteriAsync should go over all items`` () = task {
        let tq = createAsyncEnum 10
        let mutable sum = 0
        do! tq |> TaskSeq.iteriAsync (fun i _ -> sum <- sum + i)
        sum |> should equal 45 // index starts at 0
    }

    [<Fact>]
    let ``TaskSeq-iterAsync should go over all items`` () = task {
        let tq = createAsyncEnum 10
        let mutable sum = 0
        do! tq |> TaskSeq.iterAsync (fun item -> sum <- sum + item)
        sum |> should equal 55 // task-dummies started at 1
    }

    [<Fact>]
    let ``TaskSeq-map maps in correct order`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.map (fun item -> char (item + 64))
            |> TaskSeq.toSeqCachedAsync

        sq
        |> Seq.map string
        |> String.concat ""
        |> should equal "ABCDEFGHIJ"
    }

    [<Fact>]
    let ``TaskSeq-mapAsync maps in correct order`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.mapAsync (fun item -> task { return char (item + 64) })
            |> TaskSeq.toSeqCachedAsync

        sq
        |> Seq.map string
        |> String.concat ""
        |> should equal "ABCDEFGHIJ"
    }

    [<Fact>]
    let ``TaskSeq-collect operates in correct order`` () = task {
        let! sq =
            createAsyncEnum 10
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

    [<Fact>]
    let ``TaskSeq-collectSeq operates in correct order`` () = task {
        let! sq =
            createAsyncEnum 10
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

    [<Fact>]
    let ``TaskSeq-collect with empty task sequences`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.collect (fun _ -> TaskSeq.ofSeq Seq.empty)
            |> TaskSeq.toSeqCachedAsync

        Seq.isEmpty sq |> should be True
    }

    [<Fact>]
    let ``TaskSeq-collectSeq with empty sequences`` () = task {
        let! sq =
            createAsyncEnum 10
            |> TaskSeq.collectSeq (fun _ -> Seq.empty<int>)
            |> TaskSeq.toSeqCachedAsync

        Seq.isEmpty sq |> should be True
    }

    [<Fact>]
    let ``TaskSeq-empty is empty`` () = task {
        let! sq = TaskSeq.empty<string> |> TaskSeq.toSeqCachedAsync
        Seq.isEmpty sq |> should be True
    }
