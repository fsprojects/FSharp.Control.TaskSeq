module TaskSeq.Tests.Fold

open System.Text

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.fold
// TaskSeq.foldAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.fold (fun _ _ -> 42) 0 null

        assertNullArg
        <| fun () -> TaskSeq.foldAsync (fun _ _ -> Task.fromResult 42) 0 null

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

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-fold does not call folder function when empty`` variant = task {
        let mutable called = false

        let! _ =
            Gen.getEmptyVariant variant
            |> TaskSeq.fold
                (fun state _ ->
                    called <- true
                    state)
                0

        called |> should be False
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldAsync does not call folder function when empty`` variant = task {
        let mutable called = false

        let! _ =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldAsync
                (fun state _ -> task {
                    called <- true
                    return state
                })
                0

        called |> should be False
    }

module Functionality =
    [<Fact>]
    let ``TaskSeq-fold calls folder exactly N times for N elements`` () = task {
        let mutable callCount = 0

        let! _ =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.fold
                (fun acc item ->
                    callCount <- callCount + 1
                    acc + item)
                0

        callCount |> should equal 5
    }

    [<Fact>]
    let ``TaskSeq-foldAsync calls folder exactly N times for N elements`` () = task {
        let mutable callCount = 0

        let! _ =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldAsync
                (fun acc item -> task {
                    callCount <- callCount + 1
                    return acc + item
                })
                0

        callCount |> should equal 5
    }

    [<Fact>]
    let ``TaskSeq-fold over singleton calls folder once`` () = task {
        let mutable callCount = 0

        let! result =
            TaskSeq.singleton 42
            |> TaskSeq.fold
                (fun acc item ->
                    callCount <- callCount + 1
                    acc + item)
                0

        result |> should equal 42
        callCount |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-fold over two elements calls folder twice`` () = task {
        let mutable callCount = 0

        let! result =
            taskSeq {
                yield 10
                yield 20
            }
            |> TaskSeq.fold
                (fun acc item ->
                    callCount <- callCount + 1
                    acc + item)
                0

        result |> should equal 30
        callCount |> should equal 2
    }

    [<Fact>]
    let ``TaskSeq-fold is left-associative: applies folder left-to-right`` () = task {
        // For non-commutative ops like string concat, order matters.
        // fold f s [a;b;c] = f (f (f s a) b) c
        let! result =
            TaskSeq.ofList [ "b"; "c"; "d" ]
            |> TaskSeq.fold (fun acc item -> acc + item) "a"

        result |> should equal "abcd"
    }

    [<Fact>]
    let ``TaskSeq-foldAsync is left-associative: applies folder left-to-right`` () = task {
        let! result =
            TaskSeq.ofList [ "b"; "c"; "d" ]
            |> TaskSeq.foldAsync (fun acc item -> task { return acc + item }) "a"

        result |> should equal "abcd"
    }

    [<Fact>]
    let ``TaskSeq-fold with null initial state works for reference types`` () = task {
        let! result =
            TaskSeq.ofList [ "hello"; " "; "world" ]
            |> TaskSeq.fold
                (fun acc item ->
                    match acc with
                    | null -> item
                    | _ -> acc + item)
                null

        result |> should equal "hello world"
    }

    [<Fact>]
    let ``TaskSeq-foldAsync and fold return the same result for pure functions`` () = task {
        let input = [ 1..10 ]

        let! syncResult =
            TaskSeq.ofList input
            |> TaskSeq.fold (fun acc item -> acc + item) 0

        let! asyncResult =
            TaskSeq.ofList input
            |> TaskSeq.foldAsync (fun acc item -> task { return acc + item }) 0

        syncResult |> should equal asyncResult
    }

    [<Fact>]
    let ``TaskSeq-fold accumulates a list in correct order`` () = task {
        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.fold (fun acc item -> acc @ [ item ]) []

        result |> should equal [ 1; 2; 3; 4; 5 ]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-fold sum over immutable variants`` variant = task {
        // items are 1..10; sum = 55
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.fold (fun acc item -> acc + item) 0

        result |> should equal 55
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-foldAsync sum over immutable variants`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.foldAsync (fun acc item -> task { return acc + item }) 0

        result |> should equal 55
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-fold folds with every item`` variant = task {
        let! letters =
            (StringBuilder(), Gen.getSeqImmutable variant)
            ||> TaskSeq.fold (fun state item -> state.Append(char item + '@'))

        letters.ToString() |> should equal "ABCDEFGHIJ"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-foldAsync folds with every item`` variant = task {
        let! letters =
            (StringBuilder(), Gen.getSeqImmutable variant)
            ||> TaskSeq.foldAsync (fun state item -> task { return state.Append(char item + '@') })


        letters.ToString() |> should equal "ABCDEFGHIJ"
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
