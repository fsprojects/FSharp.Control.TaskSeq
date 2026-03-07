module TaskSeq.Tests.MapFold

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.mapFold
// TaskSeq.mapFoldAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.mapFold (fun _ item -> string item, 0) 0 null

        assertNullArg
        <| fun () -> TaskSeq.mapFoldAsync (fun _ item -> Task.fromResult (string item, 0)) 0 null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-mapFold on empty returns empty array with initial state`` variant = task {
        let! results, finalState =
            Gen.getEmptyVariant variant
            |> TaskSeq.mapFold (fun state item -> item * 2, state + item) 0

        results |> should equal [||]
        finalState |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-mapFoldAsync on empty returns empty array with initial state`` variant = task {
        let! results, finalState =
            Gen.getEmptyVariant variant
            |> TaskSeq.mapFoldAsync (fun state item -> task { return item * 2, state + item }) 0

        results |> should equal [||]
        finalState |> should equal 0
    }

module Functionality =
    [<Fact>]
    let ``TaskSeq-mapFold maps elements while threading state`` () = task {
        // mapFold: map each element to its double, sum all originals as state
        let! results, finalState =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.mapFold (fun state item -> item * 2, state + item) 0

        results |> should equal [| 2; 4; 6; 8; 10 |]
        finalState |> should equal 15 // 1+2+3+4+5
    }

    [<Fact>]
    let ``TaskSeq-mapFoldAsync maps elements while threading state`` () = task {
        let! results, finalState =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.mapFoldAsync (fun state item -> task { return item * 2, state + item }) 0

        results |> should equal [| 2; 4; 6; 8; 10 |]
        finalState |> should equal 15
    }

    [<Fact>]
    let ``TaskSeq-mapFold returns array of same length as source`` () = task {
        let! results, _ =
            TaskSeq.ofList [ 'a'; 'b'; 'c' ]
            |> TaskSeq.mapFold (fun idx c -> string c, idx + 1) 0

        results |> should equal [| "a"; "b"; "c" |]
    }

    [<Fact>]
    let ``TaskSeq-mapFold single element returns singleton array and updated state`` () = task {
        let! results, finalState =
            TaskSeq.singleton 42
            |> TaskSeq.mapFold (fun state item -> item + 1, state + item) 10

        results |> should equal [| 43 |]
        finalState |> should equal 52
    }

    [<Fact>]
    let ``TaskSeq-mapFold state threads through in order`` () = task {
        // Build running index as state; mapped element is (index, item) pair
        let! results, finalState =
            TaskSeq.ofList [ 10; 20; 30 ]
            |> TaskSeq.mapFold (fun idx item -> (idx, item), idx + 1) 0

        results |> should equal [| (0, 10); (1, 20); (2, 30) |]
        finalState |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-mapFoldAsync state threads through in order`` () = task {
        let! results, finalState =
            TaskSeq.ofList [ 10; 20; 30 ]
            |> TaskSeq.mapFoldAsync (fun idx item -> task { return (idx, item), idx + 1 }) 0

        results |> should equal [| (0, 10); (1, 20); (2, 30) |]
        finalState |> should equal 3
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-mapFold accumulates correctly across variants`` variant = task {
        // Input 1..10; mapped = item*item; state = running sum
        let! results, finalState =
            Gen.getSeqImmutable variant
            |> TaskSeq.mapFold (fun acc item -> item * item, acc + item) 0

        results
        |> should equal [| 1; 4; 9; 16; 25; 36; 49; 64; 81; 100 |]

        finalState |> should equal 55 // 1+2+...+10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-mapFoldAsync accumulates correctly across variants`` variant = task {
        let! results, finalState =
            Gen.getSeqImmutable variant
            |> TaskSeq.mapFoldAsync (fun acc item -> task { return item * item, acc + item }) 0

        results
        |> should equal [| 1; 4; 9; 16; 25; 36; 49; 64; 81; 100 |]

        finalState |> should equal 55
    }

    [<Fact>]
    let ``TaskSeq-mapFold result matches equivalent List.mapFold`` () = task {
        let items = [ 1; 2; 3; 4; 5 ]

        let listResults, listState = List.mapFold (fun state item -> item + state, state + item) 0 items

        let! taskResults, taskState =
            TaskSeq.ofList items
            |> TaskSeq.mapFold (fun state item -> item + state, state + item) 0

        taskResults |> should equal (Array.ofList listResults)
        taskState |> should equal listState
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-mapFold second iteration sees next batch of side-effect values`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first, firstState =
            ts
            |> TaskSeq.mapFold (fun acc item -> item * 2, acc + item) 0

        first
        |> should equal [| 2; 4; 6; 8; 10; 12; 14; 16; 18; 20 |]

        firstState |> should equal 55

        // side-effect sequences yield next 10 items (11..20) on second consumption
        let! second, secondState =
            ts
            |> TaskSeq.mapFold (fun acc item -> item * 2, acc + item) 0

        second
        |> should equal [| 22; 24; 26; 28; 30; 32; 34; 36; 38; 40 |]

        secondState |> should equal 155 // 11+12+...+20
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-mapFoldAsync second iteration sees next batch of side-effect values`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first, firstState =
            ts
            |> TaskSeq.mapFoldAsync (fun acc item -> task { return item * 2, acc + item }) 0

        first
        |> should equal [| 2; 4; 6; 8; 10; 12; 14; 16; 18; 20 |]

        firstState |> should equal 55

        let! second, secondState =
            ts
            |> TaskSeq.mapFoldAsync (fun acc item -> task { return item * 2, acc + item }) 0

        second
        |> should equal [| 22; 24; 26; 28; 30; 32; 34; 36; 38; 40 |]

        secondState |> should equal 155
    }
