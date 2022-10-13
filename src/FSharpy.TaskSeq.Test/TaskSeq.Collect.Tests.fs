module FSharpy.Tests.Collect

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

[<Fact>]
let ``TaskSeq-collect operates in correct order`` () = task {
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

[<Fact>]
let ``TaskSeq-collectSeq operates in correct order`` () = task {
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

[<Fact>]
let ``TaskSeq-collect with empty task sequences`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.collect (fun _ -> TaskSeq.ofSeq Seq.empty)
        |> TaskSeq.toSeqCachedAsync

    Seq.isEmpty sq |> should be True
}

[<Fact>]
let ``TaskSeq-collectSeq with empty sequences`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.collectSeq (fun _ -> Seq.empty<int>)
        |> TaskSeq.toSeqCachedAsync

    Seq.isEmpty sq |> should be True
}

[<Fact>]
let ``TaskSeq-empty is empty`` () = task {
    let! sq = TaskSeq.empty<string> |> TaskSeq.toSeqCachedAsync
    Seq.isEmpty sq |> should be True
}
