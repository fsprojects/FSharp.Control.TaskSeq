module TaskSeq.Tests.Choose

open System
open System.Threading.Tasks

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.choose
// TaskSeq.chooseAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.choose (fun _ -> None) null

        assertNullArg
        <| fun () -> TaskSeq.chooseAsync (fun _ -> Task.fromResult None) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-choose`` variant = task {
        let! empty =
            Gen.getEmptyVariant variant
            |> TaskSeq.choose (fun _ -> Some 42)
            |> TaskSeq.toListAsync

        List.isEmpty empty |> should be True
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chooseAsync`` variant = task {
        let! empty =
            Gen.getEmptyVariant variant
            |> TaskSeq.chooseAsync (fun _ -> task { return Some 42 })
            |> TaskSeq.toListAsync

        List.isEmpty empty |> should be True
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-choose can convert and filter`` variant = task {
        let chooser number = if number <= 5 then Some(char number + '@') else None
        let ts = Gen.getSeqImmutable variant

        let! letters1 = TaskSeq.choose chooser ts |> TaskSeq.toArrayAsync
        let! letters2 = TaskSeq.choose chooser ts |> TaskSeq.toArrayAsync

        String letters1 |> should equal "ABCDE"
        String letters2 |> should equal "ABCDE"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chooseAsync can convert and filter`` variant = task {
        let chooser number = task { return if number <= 5 then Some(char number + '@') else None }
        let ts = Gen.getSeqImmutable variant

        let! letters1 = TaskSeq.chooseAsync chooser ts |> TaskSeq.toArrayAsync
        let! letters2 = TaskSeq.chooseAsync chooser ts |> TaskSeq.toArrayAsync

        String letters1 |> should equal "ABCDE"
        String letters2 |> should equal "ABCDE"
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-choose applied multiple times`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let chooser x number = if number <= x then Some(char number + '@') else None

        let! lettersA = ts |> TaskSeq.choose (chooser 5) |> TaskSeq.toArrayAsync
        let! lettersK = ts |> TaskSeq.choose (chooser 15) |> TaskSeq.toArrayAsync
        let! lettersU = ts |> TaskSeq.choose (chooser 25) |> TaskSeq.toArrayAsync

        String lettersA |> should equal "ABCDE"
        String lettersK |> should equal "KLMNO"
        String lettersU |> should equal "UVWXY"
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chooseAsync applied multiple times`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let chooser x number = task { return if number <= x then Some(char number + '@') else None }

        let! lettersA = TaskSeq.chooseAsync (chooser 5) ts |> TaskSeq.toArrayAsync
        let! lettersK = TaskSeq.chooseAsync (chooser 15) ts |> TaskSeq.toArrayAsync
        let! lettersU = TaskSeq.chooseAsync (chooser 25) ts |> TaskSeq.toArrayAsync

        String lettersA |> should equal "ABCDE"
        String lettersK |> should equal "KLMNO"
        String lettersU |> should equal "UVWXY"
    }
