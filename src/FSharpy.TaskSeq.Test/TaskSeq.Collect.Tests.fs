module FSharpy.Tests.Collect

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

let validateSequence sequence =
    sequence
    |> Seq.map string
    |> String.concat ""
    |> should equal "ABBCCDDEEFFGGHHIIJJK"

[<Fact>]
let ``TaskSeq-collect operates in correct order`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.collect (fun item -> taskSeq {
            yield char (item + 64)
            yield char (item + 65)
        })
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-collectAsync operates in correct order`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.collectAsync (fun item -> task {
            return taskSeq {
                yield char (item + 64)
                yield char (item + 65)
            }
        })
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
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

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-collectSeq with arrays operates in correct order`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.collectSeq (fun item -> [| char (item + 64); char (item + 65) |])
        |> TaskSeq.toArrayAsync

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-collectSeqAsync operates in correct order`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.collectSeqAsync (fun item -> task {
            return seq {
                yield char (item + 64)
                yield char (item + 65)
            }
        })
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-collectSeqAsync with arrays operates in correct order`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.collectSeqAsync (fun item -> task { return [| char (item + 64); char (item + 65) |] })
        |> TaskSeq.toArrayAsync

    validateSequence sq
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
let ``TaskSeq-collectAsync with empty task sequences`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.collectAsync (fun _ -> task { return TaskSeq.empty<string> })
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
let ``TaskSeq-collectSeqAsync with empty sequences`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.collectSeqAsync (fun _ -> task { return Array.empty<int> })
        |> TaskSeq.toSeqCachedAsync

    Seq.isEmpty sq |> should be True
}
