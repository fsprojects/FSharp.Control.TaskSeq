module TaskSeq.Tests.FoldUntil

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.foldUntil
// TaskSeq.foldUntilAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.foldUntil (fun _ _ -> Continue 42) 0 null

        assertNullArg
        <| fun () -> TaskSeq.foldUntilAsync (fun _ _ -> Task.fromResult (Continue 42)) 0 null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldUntil returns initial state when empty`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldUntil (fun _ item -> Continue(item + 1)) -1

        result |> should equal -1
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldUntilAsync returns initial state when empty`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldUntilAsync (fun _ item -> task { return Continue(item + 1) }) -1

        result |> should equal -1
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldUntil does not call folder when empty`` variant = task {
        let mutable called = false

        let! _ =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldUntil
                (fun state _ ->
                    called <- true
                    Continue state)
                0

        called |> should be False
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldUntilAsync does not call folder when empty`` variant = task {
        let mutable called = false

        let! _ =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldUntilAsync
                (fun state _ -> task {
                    called <- true
                    return Continue state
                })
                0

        called |> should be False
    }

module Functionality =
    [<Fact>]
    let ``TaskSeq-foldUntil with all Continue behaves like fold`` () = task {
        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldUntil (fun acc item -> Continue(acc + item)) 0

        result |> should equal 15
    }

    [<Fact>]
    let ``TaskSeq-foldUntilAsync with all Continue behaves like foldAsync`` () = task {
        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldUntilAsync (fun acc item -> task { return Continue(acc + item) }) 0

        result |> should equal 15
    }

    [<Fact>]
    let ``TaskSeq-foldUntil is left-associative like fold`` () = task {
        let! result =
            TaskSeq.ofList [ "b"; "c"; "d" ]
            |> TaskSeq.foldUntil (fun acc item -> Continue(acc + item)) "a"

        result |> should equal "abcd"
    }

    [<Fact>]
    let ``TaskSeq-foldUntilAsync is left-associative like foldAsync`` () = task {
        let! result =
            TaskSeq.ofList [ "b"; "c"; "d" ]
            |> TaskSeq.foldUntilAsync (fun acc item -> task { return Continue(acc + item) }) "a"

        result |> should equal "abcd"
    }

module Halt =
    [<Fact>]
    let ``TaskSeq-foldUntil Halt on first element stops immediately`` () = task {
        let mutable callCount = 0

        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldUntil
                (fun _ item ->
                    callCount <- callCount + 1
                    Halt item)
                0

        result |> should equal 1
        callCount |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-foldUntilAsync Halt on first element stops immediately`` () = task {
        let mutable callCount = 0

        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldUntilAsync
                (fun _ item -> task {
                    callCount <- callCount + 1
                    return Halt item
                })
                0

        result |> should equal 1
        callCount |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-foldUntil halts mid-sequence, preserving halt state`` () = task {
        // Sum until running total exceeds 5, then halt with the overshoot.
        let mutable callCount = 0

        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldUntil
                (fun acc item ->
                    callCount <- callCount + 1
                    let next = acc + item
                    if next > 5 then Halt next else Continue next)
                0

        // 1, 3, 6 — halts on the third element (6 > 5)
        result |> should equal 6
        callCount |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-foldUntilAsync halts mid-sequence, preserving halt state`` () = task {
        let mutable callCount = 0

        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldUntilAsync
                (fun acc item -> task {
                    callCount <- callCount + 1
                    let next = acc + item
                    return if next > 5 then Halt next else Continue next
                })
                0

        result |> should equal 6
        callCount |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-foldUntil does not enumerate past the halt element`` () = task {
        // Source has a side effect per pulled element; halt on the 2nd.
        let mutable pulled = 0

        let source = taskSeq {
            for i in 1..5 do
                pulled <- pulled + 1
                yield i
        }

        let! _ =
            source
            |> TaskSeq.foldUntil (fun _ item -> if item = 2 then Halt item else Continue item) 0

        // The source yielded 1 (Continue), then 2 (Halt) — we must not pull 3.
        pulled |> should equal 2
    }

    [<Fact>]
    let ``TaskSeq-foldUntilAsync does not enumerate past the halt element`` () = task {
        let mutable pulled = 0

        let source = taskSeq {
            for i in 1..5 do
                pulled <- pulled + 1
                yield i
        }

        let! _ =
            source
            |> TaskSeq.foldUntilAsync (fun _ item -> task { return if item = 2 then Halt item else Continue item }) 0

        pulled |> should equal 2
    }

    [<Fact>]
    let ``TaskSeq-foldUntil on last-element Halt is equivalent to fold`` () = task {
        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldUntil
                (fun acc item ->
                    let next = acc + item
                    if item = 5 then Halt next else Continue next)
                0

        result |> should equal 15
    }
