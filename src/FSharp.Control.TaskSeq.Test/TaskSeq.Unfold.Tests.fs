module TaskSeq.Tests.Unfold

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.unfold
// TaskSeq.unfoldAsync
//

module EmptySeq =
    [<Fact>]
    let ``TaskSeq-unfold generator returning None immediately yields empty sequence`` () = task {
        let! result = TaskSeq.unfold (fun _ -> None) 0 |> TaskSeq.toArrayAsync

        result |> should be Empty
    }

    [<Fact>]
    let ``TaskSeq-unfoldAsync generator returning None immediately yields empty sequence`` () = task {
        let! result =
            TaskSeq.unfoldAsync (fun _ -> task { return None }) 0
            |> TaskSeq.toArrayAsync

        result |> should be Empty
    }

module Functionality =
    [<Fact>]
    let ``TaskSeq-unfold generates a finite sequence`` () = task {
        // unfold 0..9
        let! result =
            TaskSeq.unfold (fun n -> if n < 10 then Some(n, n + 1) else None) 0
            |> TaskSeq.toArrayAsync

        result |> should equal [| 0..9 |]
    }

    [<Fact>]
    let ``TaskSeq-unfoldAsync generates a finite sequence`` () = task {
        let! result =
            TaskSeq.unfoldAsync (fun n -> task { return if n < 10 then Some(n, n + 1) else None }) 0
            |> TaskSeq.toArrayAsync

        result |> should equal [| 0..9 |]
    }

    [<Fact>]
    let ``TaskSeq-unfold generates a singleton sequence`` () = task {
        let! result =
            TaskSeq.unfold (fun s -> if s = 0 then Some(42, 1) else None) 0
            |> TaskSeq.toArrayAsync

        result |> should equal [| 42 |]
    }

    [<Fact>]
    let ``TaskSeq-unfoldAsync generates a singleton sequence`` () = task {
        let! result =
            TaskSeq.unfoldAsync (fun s -> task { return if s = 0 then Some(42, 1) else None }) 0
            |> TaskSeq.toArrayAsync

        result |> should equal [| 42 |]
    }

    [<Fact>]
    let ``TaskSeq-unfold uses state correctly to thread accumulator`` () = task {
        // Fibonacci: state = (a, b), yield a, new state = (b, a+b)
        let! fibs =
            TaskSeq.unfold (fun (a, b) -> if a > 100 then None else Some(a, (b, a + b))) (1, 1)
            |> TaskSeq.toArrayAsync

        fibs
        |> should equal [| 1; 1; 2; 3; 5; 8; 13; 21; 34; 55; 89 |]
    }

    [<Fact>]
    let ``TaskSeq-unfoldAsync uses state correctly to thread accumulator`` () = task {
        let! fibs =
            TaskSeq.unfoldAsync (fun (a, b) -> task { return if a > 100 then None else Some(a, (b, a + b)) }) (1, 1)
            |> TaskSeq.toArrayAsync

        fibs
        |> should equal [| 1; 1; 2; 3; 5; 8; 13; 21; 34; 55; 89 |]
    }

    [<Fact>]
    let ``TaskSeq-unfold can be truncated to limit infinite-like sequences`` () = task {
        // counters counting from 1 upward, take first 100
        let! result =
            TaskSeq.unfold (fun n -> Some(n, n + 1)) 1
            |> TaskSeq.take 100
            |> TaskSeq.toArrayAsync

        result |> should equal [| 1..100 |]
        result |> Array.length |> should equal 100
    }

    [<Fact>]
    let ``TaskSeq-unfoldAsync can be truncated to limit infinite-like sequences`` () = task {
        let! result =
            TaskSeq.unfoldAsync (fun n -> task { return Some(n, n + 1) }) 1
            |> TaskSeq.take 100
            |> TaskSeq.toArrayAsync

        result |> should equal [| 1..100 |]
        result |> Array.length |> should equal 100
    }

    [<Fact>]
    let ``TaskSeq-unfold generates string sequences from state`` () = task {
        // build "A", "B", ..., "Z"
        let! letters =
            TaskSeq.unfold (fun c -> if c > int 'Z' then None else Some(string (char c), c + 1)) (int 'A')
            |> TaskSeq.toArrayAsync

        letters
        |> should equal [| for c in 'A' .. 'Z' -> string c |]
    }

    [<Fact>]
    let ``TaskSeq-unfold calls generator exactly once per element plus one final None call`` () = task {
        let mutable callCount = 0

        let! result =
            TaskSeq.unfold
                (fun n ->
                    callCount <- callCount + 1

                    if n < 5 then Some(n, n + 1) else None)
                0
            |> TaskSeq.toArrayAsync

        result |> should equal [| 0..4 |]
        callCount |> should equal 6 // 5 Some + 1 None
    }

    [<Fact>]
    let ``TaskSeq-unfold re-iterating restarts from initial state`` () = task {
        let ts = TaskSeq.unfold (fun n -> if n < 5 then Some(n, n + 1) else None) 0

        let! first = ts |> TaskSeq.toArrayAsync
        let! second = ts |> TaskSeq.toArrayAsync

        first |> should equal second
        first |> should equal [| 0..4 |]
    }

    [<Fact>]
    let ``TaskSeq-unfoldAsync re-iterating restarts from initial state`` () = task {
        let ts = TaskSeq.unfoldAsync (fun n -> task { return if n < 5 then Some(n, n + 1) else None }) 0

        let! first = ts |> TaskSeq.toArrayAsync
        let! second = ts |> TaskSeq.toArrayAsync

        first |> should equal second
        first |> should equal [| 0..4 |]
    }
