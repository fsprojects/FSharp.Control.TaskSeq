module FSharpy.Tests.Map

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

/// Asserts that a sequence contains the char values 'A'..'J'.
let validateSequence sequence =
    sequence
    |> Seq.map string
    |> String.concat ""
    |> should equal "ABCDEFGHIJ"

/// Validates for "ABCDEFGHIJ" char sequence, or any amount of char-value higher
let validateSequenceWithOffset offset sequence =
    let expected =
        [ 'A' .. 'J' ]
        |> List.map (int >> (+) offset >> char >> string)
        |> String.concat ""

    sequence
    |> Seq.map string
    |> String.concat ""
    |> should equal expected

[<Fact>]
let ``TaskSeq-map maps in correct order`` () = task {
    let! sq =
        Gen.sideEffectTaskSeq 10
        |> TaskSeq.map (fun item -> char (item + 64))
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-mapi maps in correct order`` () = task {
    let! sq =
        Gen.sideEffectTaskSeq 10
        |> TaskSeq.mapi (fun i _ -> char (i + 65))
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-map can access mutables which are mutated in correct order`` () = task {
    let mutable sum = 0

    let! sq =
        Gen.sideEffectTaskSeq 10
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
        Gen.sideEffectTaskSeq 10
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
        Gen.sideEffectTaskSeq 10
        |> TaskSeq.mapAsync (fun item -> task { return char (item + 64) })
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}

[<Fact>]
let ``TaskSeq-mapAsync can map the same sequence multiple times`` () = task {
    let mapAndCache =
        TaskSeq.mapAsync (fun item -> task { return char (item + 64) })
        >> TaskSeq.toSeqCachedAsync

    let ts = Gen.sideEffectTaskSeq_Sequential 10

    let! result1 = mapAndCache ts
    let! result2 = mapAndCache ts
    let! result3 = mapAndCache ts
    let! result4 = mapAndCache ts
    validateSequence result1

    // each time we do GetAsyncEnumerator(), and go through the whole sequence,
    // the whole sequence gets re-evaluated, causing our +1 side-effect to run again.
    validateSequenceWithOffset 10 result2 // the mutable is 10 higher
    validateSequenceWithOffset 20 result3 // again
    validateSequenceWithOffset 30 result4 // again
}

[<Fact>]
let ``TaskSeq-mapiAsync maps in correct order`` () = task {
    let! sq =
        Gen.sideEffectTaskSeq 10
        |> TaskSeq.mapiAsync (fun i _ -> task { return char (i + 65) })
        |> TaskSeq.toSeqCachedAsync

    validateSequence sq
}


[<Fact>]
let ``TaskSeq-mapAsync can access mutables which are mutated in correct order`` () = task {
    let mutable sum = 0

    let! sq =
        Gen.sideEffectTaskSeq 10
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
        Gen.sideEffectTaskSeq 10
        |> TaskSeq.mapiAsync (fun i _ -> task {
            data <- char (i + 65)
            return data
        })
        |> TaskSeq.toSeqCachedAsync

    data |> should equal (char 74)
    validateSequence sq
}
