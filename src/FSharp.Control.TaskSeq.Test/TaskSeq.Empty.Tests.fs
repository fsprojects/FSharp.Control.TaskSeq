module TaskSeq.Tests.Empty

open System.Threading.Tasks
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control


[<Fact>]
let ``TaskSeq-empty returns an empty sequence`` () = task {
    let! sq = TaskSeq.empty<string> |> TaskSeq.toListAsync
    Seq.isEmpty sq |> should be True
    Seq.length sq |> should equal 0
}

[<Fact>]
let ``TaskSeq-empty returns an empty sequence - variant`` () = task {
    let! isEmpty = TaskSeq.empty<string> |> TaskSeq.isEmpty
    isEmpty |> should be True
}

[<Fact>]
let ``TaskSeq-empty in a taskSeq context`` () = task {
    let! sq =
        taskSeq { yield! TaskSeq.empty<string> }
        |> TaskSeq.toArrayAsync

    Array.isEmpty sq |> should be True
}

[<Fact>]
let ``TaskSeq-empty of unit in a taskSeq context`` () = task {
    let! sq =
        taskSeq { yield! TaskSeq.empty<unit> }
        |> TaskSeq.toArrayAsync

    Array.isEmpty sq |> should be True
}

[<Fact>]
let ``TaskSeq-empty of more complex type in a taskSeq context`` () = task {
    let! sq =
        taskSeq { yield! TaskSeq.empty<Result<Task<string>, int>> }
        |> TaskSeq.toArrayAsync

    Array.isEmpty sq |> should be True
}

[<Fact>]
let ``TaskSeq-empty multiple times in a taskSeq context`` () = task {
    let! sq =
        taskSeq {
            yield! TaskSeq.empty<string>
            yield! TaskSeq.empty<string>
            yield! TaskSeq.empty<string>
            yield! TaskSeq.empty<string>
            yield! TaskSeq.empty<string>
        }
        |> TaskSeq.toArrayAsync

    Array.isEmpty sq |> should be True
}
