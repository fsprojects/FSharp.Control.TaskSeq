module FSharpy.TaskSeq.Tests.``Utility functions``

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
