module TaskSeq.Tests.Scan

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.scan
// TaskSeq.scanAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.scan (fun _ _ -> 42) 0 null

        assertNullArg
        <| fun () -> TaskSeq.scanAsync (fun _ _ -> Task.fromResult 42) 0 null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-scan on empty returns singleton initial state`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.scan (fun acc _ -> acc + 1) 0
            |> TaskSeq.toListAsync

        result |> should equal [ 0 ]
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-scanAsync on empty returns singleton initial state`` variant = task {
        let! result =
            Gen.getEmptyVariant variant
            |> TaskSeq.scanAsync (fun acc _ -> task { return acc + 1 }) 0
            |> TaskSeq.toListAsync

        result |> should equal [ 0 ]
    }

module Functionality =
    [<Fact>]
    let ``TaskSeq-scan yields initial state then each intermediate state`` () = task {
        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.scan (fun acc item -> acc + item) 0
            |> TaskSeq.toListAsync

        // N=5 elements → N+1=6 output elements
        result |> should equal [ 0; 1; 3; 6; 10; 15 ]
    }

    [<Fact>]
    let ``TaskSeq-scanAsync yields initial state then each intermediate state`` () = task {
        let! result =
            TaskSeq.ofList [ 1; 2; 3; 4; 5 ]
            |> TaskSeq.scanAsync (fun acc item -> task { return acc + item }) 0
            |> TaskSeq.toListAsync

        result |> should equal [ 0; 1; 3; 6; 10; 15 ]
    }

    [<Fact>]
    let ``TaskSeq-scan output length is input length plus one`` () = task {
        let input = TaskSeq.ofList [ 'a'; 'b'; 'c' ]

        let! result =
            input
            |> TaskSeq.scan (fun acc c -> acc + string c) ""
            |> TaskSeq.toListAsync

        result |> should equal [ ""; "a"; "ab"; "abc" ]
    }

    [<Fact>]
    let ``TaskSeq-scanAsync output length is input length plus one`` () = task {
        let input = TaskSeq.ofList [ 'a'; 'b'; 'c' ]

        let! result =
            input
            |> TaskSeq.scanAsync (fun acc c -> task { return acc + string c }) ""
            |> TaskSeq.toListAsync

        result |> should equal [ ""; "a"; "ab"; "abc" ]
    }

    [<Fact>]
    let ``TaskSeq-scan with single element returns two-element result`` () = task {
        let! result =
            TaskSeq.singleton 42
            |> TaskSeq.scan (fun acc item -> acc + item) 10
            |> TaskSeq.toListAsync

        result |> should equal [ 10; 52 ]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-scan accumulates correctly across variants`` variant = task {
        // Input is 1..10; cumulative sums: 0, 1, 3, 6, 10, 15, 21, 28, 36, 45, 55
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.scan (fun acc item -> acc + item) 0
            |> TaskSeq.toListAsync

        result
        |> should equal [ 0; 1; 3; 6; 10; 15; 21; 28; 36; 45; 55 ]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-scanAsync accumulates correctly across variants`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.scanAsync (fun acc item -> task { return acc + item }) 0
            |> TaskSeq.toListAsync

        result
        |> should equal [ 0; 1; 3; 6; 10; 15; 21; 28; 36; 45; 55 ]
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-scan second iteration accumulates from fresh start`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first =
            ts
            |> TaskSeq.scan (fun acc item -> acc + item) 0
            |> TaskSeq.toListAsync

        first
        |> should equal [ 0; 1; 3; 6; 10; 15; 21; 28; 36; 45; 55 ]

        let! second =
            ts
            |> TaskSeq.scan (fun acc item -> acc + item) 0
            |> TaskSeq.toListAsync

        // side-effect sequences yield next 10 items (11..20)
        second
        |> should equal [ 0; 11; 23; 36; 50; 65; 81; 98; 116; 135; 155 ]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-scanAsync second iteration accumulates from fresh start`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first =
            ts
            |> TaskSeq.scanAsync (fun acc item -> task { return acc + item }) 0
            |> TaskSeq.toListAsync

        first
        |> should equal [ 0; 1; 3; 6; 10; 15; 21; 28; 36; 45; 55 ]

        let! second =
            ts
            |> TaskSeq.scanAsync (fun acc item -> task { return acc + item }) 0
            |> TaskSeq.toListAsync

        second
        |> should equal [ 0; 11; 23; 36; 50; 65; 81; 98; 116; 135; 155 ]
    }
