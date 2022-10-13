module FSharpy.Tests.``Utility functions``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Fact>]
let ``TaskSeq-empty is empty`` () = task {
    let! sq = TaskSeq.empty<string> |> TaskSeq.toSeqCachedAsync
    Seq.isEmpty sq |> should be True
    Seq.length sq |> should equal 0
}

[<Fact>]
let ``TaskSeq-isEmpty is returns true for empty`` () = TaskSeq.empty<string> |> TaskSeq.isEmpty |> should be True

[<Fact>]
let ``TaskSeq-isEmpty returns false for non-empty`` () = taskSeq { yield 10 } |> TaskSeq.isEmpty |> should be False

[<Fact>]
let ``TaskSeq-isEmptyAsync returns true for empty`` () = task {
    let! isEmpty = TaskSeq.empty<string> |> TaskSeq.isEmptyAsync
    isEmpty |> should be True
}

[<Fact>]
let ``TaskSeq-isEmptyAsync returns false for non-empty`` () = task {
    let! isEmpty = taskSeq { yield 42 } |> TaskSeq.isEmptyAsync
    isEmpty |> should be False
}
