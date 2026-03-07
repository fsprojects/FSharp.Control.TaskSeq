module TaskSeq.Tests.Choose

open System

open Xunit
open FsUnit.Xunit

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

module Immutable2 =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-choose returns all when chooser always returns Some`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! xs = ts |> TaskSeq.choose Some |> TaskSeq.toArrayAsync
        xs |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chooseAsync returns all when chooser always returns Some`` variant = task {
        let ts = Gen.getSeqImmutable variant

        let! xs =
            ts
            |> TaskSeq.chooseAsync (fun x -> task { return Some x })
            |> TaskSeq.toArrayAsync

        xs |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-choose returns empty when chooser always returns None`` variant = task {
        let ts = Gen.getSeqImmutable variant

        do! ts |> TaskSeq.choose (fun _ -> None) |> verifyEmpty
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chooseAsync returns empty when chooser always returns None`` variant = task {
        let ts = Gen.getSeqImmutable variant

        do!
            ts
            |> TaskSeq.chooseAsync (fun _ -> task { return None })
            |> verifyEmpty
    }

    [<Fact>]
    let ``TaskSeq-choose with singleton sequence and Some chooser returns singleton`` () = task {
        let! xs =
            taskSeq { yield 42 }
            |> TaskSeq.choose (fun x -> Some(x * 2))
            |> TaskSeq.toListAsync

        xs |> should equal [ 84 ]
    }

    [<Fact>]
    let ``TaskSeq-choose with singleton sequence and None chooser returns empty`` () =
        taskSeq { yield 42 }
        |> TaskSeq.choose (fun _ -> None)
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-choose can change the element type`` () = task {
        // choose maps int -> string option, verifying type-changing behavior
        let chooser n = if n % 2 = 0 then Some(sprintf "even-%d" n) else None

        let! xs =
            taskSeq { yield! [ 1..6 ] }
            |> TaskSeq.choose chooser
            |> TaskSeq.toListAsync

        xs |> should equal [ "even-2"; "even-4"; "even-6" ]
    }

    [<Fact>]
    let ``TaskSeq-chooseAsync can change the element type`` () = task {
        let chooser n = task { return if n % 2 = 0 then Some(sprintf "even-%d" n) else None }

        let! xs =
            taskSeq { yield! [ 1..6 ] }
            |> TaskSeq.chooseAsync chooser
            |> TaskSeq.toListAsync

        xs |> should equal [ "even-2"; "even-4"; "even-6" ]
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

    [<Fact>]
    let ``TaskSeq-choose evaluates each source element exactly once`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..5 do
                count <- count + 1
                yield i
        }

        let! xs =
            ts
            |> TaskSeq.choose (fun x -> if x < 3 then Some x else None)
            |> TaskSeq.toListAsync

        count |> should equal 5 // all 5 elements were visited
        xs |> should equal [ 1; 2 ]
    }

    [<Fact>]
    let ``TaskSeq-chooseAsync evaluates each source element exactly once`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..5 do
                count <- count + 1
                yield i
        }

        let! xs =
            ts
            |> TaskSeq.chooseAsync (fun x -> task { return if x < 3 then Some x else None })
            |> TaskSeq.toListAsync

        count |> should equal 5
        xs |> should equal [ 1; 2 ]
    }
