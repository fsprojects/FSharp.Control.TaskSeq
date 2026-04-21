module TaskSeq.Tests.FoldWhile

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.foldWhile
// TaskSeq.foldWhileAsync
//
// Semantics match TaskSeq.takeWhile: the predicate is evaluated against (state, element)
// before that element is folded in. When the predicate returns false, iteration halts
// without folding that element, and no further elements are enumerated.
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.foldWhile (fun _ _ -> true) (fun _ item -> item + 1) 0 null

        assertNullArg
        <| fun () ->
            TaskSeq.foldWhileAsync
                (fun _ _ -> Task.fromResult true)
                (fun _ item -> Task.fromResult (item + 1))
                0
                null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldWhile returns initial state when empty`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldWhile (fun _ _ -> true) (fun _ item -> item + 1) -1

        result |> should equal -1
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldWhileAsync returns initial state when empty`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldWhileAsync
                (fun _ _ -> Task.fromResult true)
                (fun _ item -> Task.fromResult (item + 1))
                -1

        result |> should equal -1
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldWhile does not call predicate or folder when empty`` variant = task {
        let mutable predicateCalled = false
        let mutable folderCalled = false

        let! _ =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldWhile
                (fun _ _ ->
                    predicateCalled <- true
                    true)
                (fun state _ ->
                    folderCalled <- true
                    state)
                0

        predicateCalled |> should be False
        folderCalled |> should be False
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-foldWhileAsync does not call predicate or folder when empty`` variant = task {
        let mutable predicateCalled = false
        let mutable folderCalled = false

        let! _ =
            Gen.getEmptyVariant variant
            |> TaskSeq.foldWhileAsync
                (fun _ _ -> task {
                    predicateCalled <- true
                    return true
                })
                (fun state _ -> task {
                    folderCalled <- true
                    return state
                })
                0

        predicateCalled |> should be False
        folderCalled |> should be False
    }

module Functionality =
    [<Fact>]
    let ``TaskSeq-foldWhile with always-true predicate behaves like fold`` () = task {
        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldWhile (fun _ _ -> true) (fun acc item -> acc + item) 0

        result |> should equal 15
    }

    [<Fact>]
    let ``TaskSeq-foldWhileAsync with always-true predicate behaves like foldAsync`` () = task {
        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldWhileAsync
                (fun _ _ -> Task.fromResult true)
                (fun acc item -> Task.fromResult (acc + item))
                0

        result |> should equal 15
    }

    [<Fact>]
    let ``TaskSeq-foldWhile is left-associative like fold`` () = task {
        let! result =
            TaskSeq.ofList [ "b"; "c"; "d" ]
            |> TaskSeq.foldWhile (fun _ _ -> true) (fun acc item -> acc + item) "a"

        result |> should equal "abcd"
    }

    [<Fact>]
    let ``TaskSeq-foldWhileAsync is left-associative like foldAsync`` () = task {
        let! result =
            TaskSeq.ofList [ "b"; "c"; "d" ]
            |> TaskSeq.foldWhileAsync
                (fun _ _ -> Task.fromResult true)
                (fun acc item -> Task.fromResult (acc + item))
                "a"

        result |> should equal "abcd"
    }

module Halt =
    [<Fact>]
    let ``TaskSeq-foldWhile stops immediately when predicate is false on first element`` () = task {
        let mutable predicateCalls = 0
        let mutable folderCalls = 0

        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldWhile
                (fun _ _ ->
                    predicateCalls <- predicateCalls + 1
                    false)
                (fun _ item ->
                    folderCalls <- folderCalls + 1
                    item)
                0

        result |> should equal 0
        predicateCalls |> should equal 1
        folderCalls |> should equal 0
    }

    [<Fact>]
    let ``TaskSeq-foldWhileAsync stops immediately when predicate is false on first element`` () = task {
        let mutable predicateCalls = 0
        let mutable folderCalls = 0

        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldWhileAsync
                (fun _ _ -> task {
                    predicateCalls <- predicateCalls + 1
                    return false
                })
                (fun _ item -> task {
                    folderCalls <- folderCalls + 1
                    return item
                })
                0

        result |> should equal 0
        predicateCalls |> should equal 1
        folderCalls |> should equal 0
    }

    [<Fact>]
    let ``TaskSeq-foldWhile halts mid-sequence without folding the halting element`` () = task {
        // Sum while adding the next element would keep the total <= 5. Once adding
        // the element would overshoot, stop — that element is NOT folded in.
        let mutable predicateCalls = 0
        let mutable folderCalls = 0

        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldWhile
                (fun acc item ->
                    predicateCalls <- predicateCalls + 1
                    acc + item <= 5)
                (fun acc item ->
                    folderCalls <- folderCalls + 1
                    acc + item)
                0

        // 1 (ok, total 1), 2 (ok, total 3), 3 (would make 6 > 5, stop)
        result |> should equal 3
        predicateCalls |> should equal 3
        folderCalls |> should equal 2
    }

    [<Fact>]
    let ``TaskSeq-foldWhileAsync halts mid-sequence without folding the halting element`` () = task {
        let mutable predicateCalls = 0
        let mutable folderCalls = 0

        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldWhileAsync
                (fun acc item -> task {
                    predicateCalls <- predicateCalls + 1
                    return acc + item <= 5
                })
                (fun acc item -> task {
                    folderCalls <- folderCalls + 1
                    return acc + item
                })
                0

        result |> should equal 3
        predicateCalls |> should equal 3
        folderCalls |> should equal 2
    }

    [<Fact>]
    let ``TaskSeq-foldWhile does not enumerate past the halting element`` () = task {
        // Source has a side effect per pulled element; halt on the 3rd pull.
        let mutable pulled = 0

        let source = taskSeq {
            for i in 1..5 do
                pulled <- pulled + 1
                yield i
        }

        let! _ =
            source
            |> TaskSeq.foldWhile (fun _ item -> item < 3) (fun acc item -> acc + item) 0

        // Pull 1 (ok), pull 2 (ok), pull 3 (predicate false, stop) — must not pull 4.
        pulled |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-foldWhileAsync does not enumerate past the halting element`` () = task {
        let mutable pulled = 0

        let source = taskSeq {
            for i in 1..5 do
                pulled <- pulled + 1
                yield i
        }

        let! _ =
            source
            |> TaskSeq.foldWhileAsync
                (fun _ item -> Task.fromResult (item < 3))
                (fun acc item -> Task.fromResult (acc + item))
                0

        pulled |> should equal 3
    }

    [<Fact>]
    let ``TaskSeq-foldWhile that never halts is equivalent to fold`` () = task {
        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.foldWhile (fun _ item -> item <= 10) (fun acc item -> acc + item) 0

        result |> should equal 15
    }
