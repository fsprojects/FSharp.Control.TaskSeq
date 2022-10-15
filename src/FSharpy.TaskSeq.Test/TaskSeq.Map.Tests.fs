module FSharpy.Tests.Map

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

let validateSequence sequence =
    sequence
    |> Seq.map string
    |> String.concat ""
    |> should equal "ABCDEFGHIJ"

[<Fact>]
let ``TaskSeq-map maps in correct order`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.map (fun item -> char (item + 64))
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-mapi maps in correct order`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.mapi (fun i _ -> char (i + 65))
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-map can access mutables which are mutated in correct order`` () = task {
    let mutable sum = 0

    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.map (fun item ->
            sum <- sum + 1
            char (sum + 64))
        |> TaskSeq.toSeqCachedAsync

    sum |> should equal 10
    validateSequence sq
}

[<Fact>]
let ``TaskSeq-mapi can access mutables which are mutated in correct order`` () = task {
    let mutable sum = 0

    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.mapi (fun i _ ->
            sum <- i + 1
            char (sum + 64))
        |> TaskSeq.toSeqCachedAsync

    sum |> should equal 10
    validateSequence sq
}

[<Fact>]
let ``TaskSeq-mapAsync maps in correct order`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.mapAsync (fun item -> task { return char (item + 64) })
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-mapiAsync maps in correct order`` () = task {
    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.mapiAsync (fun i _ -> task { return char (i + 65) })
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}


[<Fact>]
let ``TaskSeq-mapAsync can access mutables which are mutated in correct order`` () = task {
    let mutable sum = 0

    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.mapAsync (fun item -> task {
            sum <- sum + 1
            return char (sum + 64)
        })
        |> TaskSeq.toSeqCachedAsync

    sum |> should equal 10
    validateSequence sq
}

[<Fact>]
let ``TaskSeq-mapiAsync can access mutables which are mutated in correct order`` () = task {
    let mutable data = '0'

    let! sq =
        createDummyTaskSeq 10
        |> TaskSeq.mapiAsync (fun i _ -> task {
            data <- char (i + 65)
            return data
        })
        |> TaskSeq.toSeqCachedAsync

    data |> should equal (char 74)
    validateSequence sq
}
