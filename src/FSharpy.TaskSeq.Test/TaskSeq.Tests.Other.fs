module FSharpy.Tests.``Other functions``

open System.Threading.Tasks
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

[<Fact>]
let ``TaskSeq-isEmpty returns false for delayed non-empty sequence`` () = task {
    let! isEmpty =
        createLongerDummyTaskSeq 200<ms> 400<ms> 3
        |> TaskSeq.isEmpty

    isEmpty |> should be False
}
