module FSharpy.Tests.Fold

open System.Text
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-fold takes state when empty`` variant = task {
        let! empty =
            Gen.getEmptyVariant variant
            |> TaskSeq.fold (fun _ item -> char (item + 64)) '_'

        empty |> should equal '_'
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldAsync takes state when empty`` variant = task {
        let! alphabet =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldAsync (fun _ item -> task { return char (item + 64) }) '_'

        alphabet |> should equal '_'
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-fold folds with every item`` variant = task {
        let! letters =
            (StringBuilder(), Gen.getSeqImmutable variant)
            ||> TaskSeq.fold (fun state item -> state.Append(char item + '@'))

        letters.ToString()
        |> should equal "ABCDEFGHIJ"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-foldAsync folds with every item`` variant = task {
        let! letters =
            (StringBuilder(), Gen.getSeqImmutable variant)
            ||> TaskSeq.foldAsync (fun state item -> task { return state.Append(char item + '@') })
                

        letters.ToString()
        |> should equal "ABCDEFGHIJ"
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-fold folds with every item, next fold has different state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! letters =
            (StringBuilder(), ts)
            ||> TaskSeq.fold (fun state item -> state.Append(char item + '@'))

        string letters |> should equal "ABCDEFGHIJ"

        let! moreLetters =
            (letters, ts)
            ||> TaskSeq.fold (fun state item -> state.Append(char item + '@'))

        string moreLetters |> should equal "ABCDEFGHIJKLMNOPQRST"
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-foldAsync folds with every item, next fold has different state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! letters =
            (StringBuilder(), ts)
            ||> TaskSeq.foldAsync (fun state item -> task { return state.Append(char item + '@') })

        string letters |> should equal "ABCDEFGHIJ"

        let! moreLetters =
            (letters, ts)
            ||> TaskSeq.foldAsync (fun state item -> task { return state.Append(char item + '@') })

        string moreLetters |> should equal "ABCDEFGHIJKLMNOPQRST"
    }

