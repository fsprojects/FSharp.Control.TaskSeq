module TaskSeq.Tests.ChooseV

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.chooseV
// TaskSeq.chooseVAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.chooseV (fun _ -> ValueNone) null

        assertNullArg
        <| fun () -> TaskSeq.chooseVAsync (fun _ -> Task.fromResult ValueNone) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chooseV`` variant = task {
        let! empty =
            Gen.getEmptyVariant variant
            |> TaskSeq.chooseV (fun _ -> ValueSome 42)
            |> TaskSeq.toListAsync

        List.isEmpty empty |> should be True
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-chooseVAsync`` variant = task {
        let! empty =
            Gen.getEmptyVariant variant
            |> TaskSeq.chooseVAsync (fun _ -> task { return ValueSome 42 })
            |> TaskSeq.toListAsync

        List.isEmpty empty |> should be True
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chooseV can convert and filter`` variant = task {
        let chooser number =
            if number <= 5 then
                ValueSome(char number + '@')
            else
                ValueNone

        let ts = Gen.getSeqImmutable variant

        let! letters1 = TaskSeq.chooseV chooser ts |> TaskSeq.toArrayAsync
        let! letters2 = TaskSeq.chooseV chooser ts |> TaskSeq.toArrayAsync

        String letters1 |> should equal "ABCDE"
        String letters2 |> should equal "ABCDE"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chooseVAsync can convert and filter`` variant = task {
        let chooser number = task {
            return
                if number <= 5 then
                    ValueSome(char number + '@')
                else
                    ValueNone
        }

        let ts = Gen.getSeqImmutable variant

        let! letters1 = TaskSeq.chooseVAsync chooser ts |> TaskSeq.toArrayAsync
        let! letters2 = TaskSeq.chooseVAsync chooser ts |> TaskSeq.toArrayAsync

        String letters1 |> should equal "ABCDE"
        String letters2 |> should equal "ABCDE"
    }

module Immutable2 =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chooseV returns all when chooser always returns ValueSome`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! xs = ts |> TaskSeq.chooseV ValueSome |> TaskSeq.toArrayAsync
        xs |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chooseVAsync returns all when chooser always returns ValueSome`` variant = task {
        let ts = Gen.getSeqImmutable variant

        let! xs =
            ts
            |> TaskSeq.chooseVAsync (fun x -> task { return ValueSome x })
            |> TaskSeq.toArrayAsync

        xs |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chooseV returns empty when chooser always returns ValueNone`` variant = task {
        let ts = Gen.getSeqImmutable variant

        do! ts |> TaskSeq.chooseV (fun _ -> ValueNone) |> verifyEmpty
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-chooseVAsync returns empty when chooser always returns ValueNone`` variant = task {
        let ts = Gen.getSeqImmutable variant

        do!
            ts
            |> TaskSeq.chooseVAsync (fun _ -> task { return ValueNone })
            |> verifyEmpty
    }

    [<Fact>]
    let ``TaskSeq-chooseV with singleton sequence and ValueSome chooser returns singleton`` () = task {
        let! xs =
            taskSeq { yield 42 }
            |> TaskSeq.chooseV (fun x -> ValueSome(x * 2))
            |> TaskSeq.toListAsync

        xs |> should equal [ 84 ]
    }

    [<Fact>]
    let ``TaskSeq-chooseV with singleton sequence and ValueNone chooser returns empty`` () =
        taskSeq { yield 42 }
        |> TaskSeq.chooseV (fun _ -> ValueNone)
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-chooseV can change the element type`` () = task {
        // choose maps int -> string voption, verifying type-changing behavior
        let chooser n =
            if n % 2 = 0 then
                ValueSome(sprintf "even-%d" n)
            else
                ValueNone

        let! xs =
            taskSeq { yield! [ 1..6 ] }
            |> TaskSeq.chooseV chooser
            |> TaskSeq.toListAsync

        xs |> should equal [ "even-2"; "even-4"; "even-6" ]
    }

    [<Fact>]
    let ``TaskSeq-chooseVAsync can change the element type`` () = task {
        let chooser n = task {
            return
                if n % 2 = 0 then
                    ValueSome(sprintf "even-%d" n)
                else
                    ValueNone
        }

        let! xs =
            taskSeq { yield! [ 1..6 ] }
            |> TaskSeq.chooseVAsync chooser
            |> TaskSeq.toListAsync

        xs |> should equal [ "even-2"; "even-4"; "even-6" ]
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chooseV applied multiple times`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let chooser x number =
            if number <= x then
                ValueSome(char number + '@')
            else
                ValueNone

        let! lettersA = ts |> TaskSeq.chooseV (chooser 5) |> TaskSeq.toArrayAsync
        let! lettersK = ts |> TaskSeq.chooseV (chooser 15) |> TaskSeq.toArrayAsync
        let! lettersU = ts |> TaskSeq.chooseV (chooser 25) |> TaskSeq.toArrayAsync

        String lettersA |> should equal "ABCDE"
        String lettersK |> should equal "KLMNO"
        String lettersU |> should equal "UVWXY"
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-chooseVAsync applied multiple times`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let chooser x number = task {
            return
                if number <= x then
                    ValueSome(char number + '@')
                else
                    ValueNone
        }

        let! lettersA = TaskSeq.chooseVAsync (chooser 5) ts |> TaskSeq.toArrayAsync
        let! lettersK = TaskSeq.chooseVAsync (chooser 15) ts |> TaskSeq.toArrayAsync
        let! lettersU = TaskSeq.chooseVAsync (chooser 25) ts |> TaskSeq.toArrayAsync

        String lettersA |> should equal "ABCDE"
        String lettersK |> should equal "KLMNO"
        String lettersU |> should equal "UVWXY"
    }

    [<Fact>]
    let ``TaskSeq-chooseV evaluates each source element exactly once`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..5 do
                count <- count + 1
                yield i
        }

        let! xs =
            ts
            |> TaskSeq.chooseV (fun x -> if x < 3 then ValueSome x else ValueNone)
            |> TaskSeq.toListAsync

        count |> should equal 5 // all 5 elements were visited
        xs |> should equal [ 1; 2 ]
    }

    [<Fact>]
    let ``TaskSeq-chooseVAsync evaluates each source element exactly once`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..5 do
                count <- count + 1
                yield i
        }

        let! xs =
            ts
            |> TaskSeq.chooseVAsync (fun x -> task { return if x < 3 then ValueSome x else ValueNone })
            |> TaskSeq.toListAsync

        count |> should equal 5
        xs |> should equal [ 1; 2 ]
    }
