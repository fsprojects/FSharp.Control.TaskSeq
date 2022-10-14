module FSharpy.Tests.``Other functions``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Fact>]
let ``TaskSeq-empty returns an empty sequence`` () = task {
    let! sq = TaskSeq.empty<string> |> TaskSeq.toSeqCachedAsync
    Seq.isEmpty sq |> should be True
    Seq.length sq |> should equal 0
}

[<Fact>]
let ``TaskSeq-isEmpty returns true for empty`` () = task {
    let! isEmpty = TaskSeq.empty<string> |> TaskSeq.isEmpty
    isEmpty |> should be True
}

[<Fact>]
let ``TaskSeq-isEmpty returns false for non-empty`` () = task {
    let! isEmpty = taskSeq { yield 42 } |> TaskSeq.isEmpty
    isEmpty |> should be False
}
