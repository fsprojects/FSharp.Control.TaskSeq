module FSharpy.Tests.Fold

open System.Text
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Fact(Timeout = 10_000)>]
let ``TaskSeq-fold folds with every item`` () = task {
    let! alphabet =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 26
        |> TaskSeq.fold (fun (state: StringBuilder) item -> state.Append(char item + '@')) (StringBuilder())

    alphabet.ToString()
    |> should equal "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-foldAsync folds with every item`` () = task {
    let! alphabet =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 26
        |> TaskSeq.foldAsync
            (fun (state: StringBuilder) item -> task { return state.Append(char item + '@') })
            (StringBuilder())

    alphabet.ToString()
    |> should equal "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-fold takes state on empty IAsyncEnumberable`` () = task {
    let! empty =
        TaskSeq.empty
        |> TaskSeq.fold (fun _ item -> char (item + 64)) '_'

    empty |> should equal '_'
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-foldAsync takes state on empty IAsyncEnumerable`` () = task {
    let! alphabet =
        TaskSeq.empty
        |> TaskSeq.foldAsync (fun _ item -> task { return char (item + 64) }) '_'

    alphabet |> should equal '_'
}
