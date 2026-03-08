module TaskSeq.Tests.Sort

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.rev
// TaskSeq.sort / sortDescending
// TaskSeq.sortBy / sortByDescending / sortByAsync / sortByDescendingAsync
// TaskSeq.sortWith
//

module RevEmpty =
    [<Fact>]
    let ``TaskSeq-rev with null source raises`` () = assertNullArg <| fun () -> TaskSeq.rev null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-rev on empty returns empty`` variant = Gen.getEmptyVariant variant |> TaskSeq.rev |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-rev on singleton returns singleton`` () = task {
        let! result = taskSeq { yield 42 } |> TaskSeq.rev |> TaskSeq.toListAsync
        result |> should equal [ 42 ]
    }

module RevImmutable =
    [<Fact>]
    let ``TaskSeq-rev reverses a simple list`` () = task {
        let! result =
            taskSeq { yield! [ 1..5 ] }
            |> TaskSeq.rev
            |> TaskSeq.toListAsync

        result |> should equal [ 5; 4; 3; 2; 1 ]
    }

    [<Fact>]
    let ``TaskSeq-rev on two elements swaps them`` () = task {
        let! result =
            taskSeq { yield! [ 10; 20 ] }
            |> TaskSeq.rev
            |> TaskSeq.toListAsync

        result |> should equal [ 20; 10 ]
    }

    [<Fact>]
    let ``TaskSeq-rev is idempotent when applied twice`` () = task {
        let original = [ 1..7 ]

        let! result =
            taskSeq { yield! original }
            |> TaskSeq.rev
            |> TaskSeq.rev
            |> TaskSeq.toListAsync

        result |> should equal original
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-rev all variants yields elements in reverse order`` variant = task {
        let! result =
            Gen.getSeqImmutable variant
            |> TaskSeq.rev
            |> TaskSeq.toListAsync

        result |> should equal [ 10; 9; 8; 7; 6; 5; 4; 3; 2; 1 ]
    }

module SortEmpty =
    [<Fact>]
    let ``TaskSeq-sort with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sort null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sort on empty returns empty`` variant = Gen.getEmptyVariant variant |> TaskSeq.sort |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-sortDescending with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sortDescending null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sortDescending on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.sortDescending
        |> verifyEmpty

module SortImmutable =
    [<Fact>]
    let ``TaskSeq-sort already-sorted sequence`` () = task {
        let! result =
            taskSeq { yield! [ 1..5 ] }
            |> TaskSeq.sort
            |> TaskSeq.toListAsync

        result |> should equal [ 1; 2; 3; 4; 5 ]
    }

    [<Fact>]
    let ``TaskSeq-sort unsorted sequence`` () = task {
        let! result =
            taskSeq { yield! [ 3; 1; 4; 1; 5; 9; 2; 6 ] }
            |> TaskSeq.sort
            |> TaskSeq.toListAsync

        result |> should equal [ 1; 1; 2; 3; 4; 5; 6; 9 ]
    }

    [<Fact>]
    let ``TaskSeq-sort reverse-sorted sequence`` () = task {
        let! result =
            taskSeq { yield! [ 5; 4; 3; 2; 1 ] }
            |> TaskSeq.sort
            |> TaskSeq.toListAsync

        result |> should equal [ 1; 2; 3; 4; 5 ]
    }

    [<Fact>]
    let ``TaskSeq-sort strings`` () = task {
        let! result =
            taskSeq { yield! [ "banana"; "apple"; "cherry" ] }
            |> TaskSeq.sort
            |> TaskSeq.toListAsync

        result |> should equal [ "apple"; "banana"; "cherry" ]
    }

    [<Fact>]
    let ``TaskSeq-sortDescending unsorted sequence`` () = task {
        let! result =
            taskSeq { yield! [ 3; 1; 4; 1; 5; 9; 2; 6 ] }
            |> TaskSeq.sortDescending
            |> TaskSeq.toListAsync

        result |> should equal [ 9; 6; 5; 4; 3; 2; 1; 1 ]
    }

    [<Fact>]
    let ``TaskSeq-sortDescending is inverse of sort`` () = task {
        let! asc =
            taskSeq { yield! [ 5; 1; 3; 2; 4 ] }
            |> TaskSeq.sort
            |> TaskSeq.toListAsync

        let! desc =
            taskSeq { yield! [ 5; 1; 3; 2; 4 ] }
            |> TaskSeq.sortDescending
            |> TaskSeq.toListAsync

        desc |> List.rev |> should equal asc
    }

module SortByEmpty =
    [<Fact>]
    let ``TaskSeq-sortBy with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sortBy id null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sortBy on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.sortBy id
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-sortByDescending with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sortByDescending id null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sortByDescending on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.sortByDescending id
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-sortByAsync with null source raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.sortByAsync (fun x -> task { return x }) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sortByAsync on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.sortByAsync (fun x -> task { return x })
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-sortByDescendingAsync with null source raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.sortByDescendingAsync (fun x -> task { return x }) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sortByDescendingAsync on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.sortByDescendingAsync (fun x -> task { return x })
        |> verifyEmpty

module SortByImmutable =
    [<Fact>]
    let ``TaskSeq-sortBy ascending by negative key reverses order`` () = task {
        let! result =
            taskSeq { yield! [ 1..5 ] }
            |> TaskSeq.sortBy (fun x -> -x)
            |> TaskSeq.toListAsync

        result |> should equal [ 5; 4; 3; 2; 1 ]
    }

    [<Fact>]
    let ``TaskSeq-sortBy sorts record fields correctly`` () = task {
        let items = [ {| Name = "Charlie"; Age = 30 |}; {| Name = "Alice"; Age = 25 |}; {| Name = "Bob"; Age = 35 |} ]

        let! byName =
            taskSeq { yield! items }
            |> TaskSeq.sortBy (fun x -> x.Name)
            |> TaskSeq.toListAsync

        let! byAge =
            taskSeq { yield! items }
            |> TaskSeq.sortBy (fun x -> x.Age)
            |> TaskSeq.toListAsync

        byName
        |> List.map (fun x -> x.Name)
        |> should equal [ "Alice"; "Bob"; "Charlie" ]

        byAge
        |> List.map (fun x -> x.Age)
        |> should equal [ 25; 30; 35 ]
    }

    [<Fact>]
    let ``TaskSeq-sortByDescending ascending by negative key gives ascending order`` () = task {
        let! result =
            taskSeq { yield! [ 3; 1; 4; 1; 5 ] }
            |> TaskSeq.sortByDescending (fun x -> -x)
            |> TaskSeq.toListAsync

        result |> should equal [ 1; 1; 3; 4; 5 ]
    }

    [<Fact>]
    let ``TaskSeq-sortByDescending sorts in descending order by projected key`` () = task {
        let! result =
            taskSeq { yield! [ "bb"; "aaa"; "c" ] }
            |> TaskSeq.sortByDescending (fun s -> s.Length)
            |> TaskSeq.toListAsync

        result
        |> List.map (fun s -> s.Length)
        |> should equal [ 3; 2; 1 ]
    }

    [<Fact>]
    let ``TaskSeq-sortByAsync yields same result as sortBy for identity async`` () = task {
        let input = [ 5; 3; 1; 4; 2 ]

        let! sync =
            taskSeq { yield! input }
            |> TaskSeq.sortBy id
            |> TaskSeq.toListAsync

        let! async' =
            taskSeq { yield! input }
            |> TaskSeq.sortByAsync (fun x -> task { return x })
            |> TaskSeq.toListAsync

        async' |> should equal sync
    }

    [<Fact>]
    let ``TaskSeq-sortByDescendingAsync yields same result as sortByDescending for identity async`` () = task {
        let input = [ 5; 3; 1; 4; 2 ]

        let! sync =
            taskSeq { yield! input }
            |> TaskSeq.sortByDescending id
            |> TaskSeq.toListAsync

        let! asyncDesc =
            taskSeq { yield! input }
            |> TaskSeq.sortByDescendingAsync (fun x -> task { return x })
            |> TaskSeq.toListAsync

        asyncDesc |> should equal sync
    }

    [<Fact>]
    let ``TaskSeq-sortByAsync evaluates projection exactly once per element`` () = task {
        let mutable callCount = 0

        let! result =
            taskSeq { yield! [ 3; 1; 2 ] }
            |> TaskSeq.sortByAsync (fun x -> task {
                callCount <- callCount + 1
                return x
            })
            |> TaskSeq.toListAsync

        callCount |> should equal 3
        result |> should equal [ 1; 2; 3 ]
    }

module SortWithEmpty =
    [<Fact>]
    let ``TaskSeq-sortWith with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sortWith compare null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-sortWith on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.sortWith compare
        |> verifyEmpty

module SortWithImmutable =
    [<Fact>]
    let ``TaskSeq-sortWith ascending with standard compare`` () = task {
        let! result =
            taskSeq { yield! [ 3; 1; 4; 1; 5; 9; 2; 6 ] }
            |> TaskSeq.sortWith compare
            |> TaskSeq.toListAsync

        result |> should equal [ 1; 1; 2; 3; 4; 5; 6; 9 ]
    }

    [<Fact>]
    let ``TaskSeq-sortWith descending with reversed compare`` () = task {
        let! result =
            taskSeq { yield! [ 3; 1; 4; 1; 5; 9; 2; 6 ] }
            |> TaskSeq.sortWith (fun a b -> compare b a)
            |> TaskSeq.toListAsync

        result |> should equal [ 9; 6; 5; 4; 3; 2; 1; 1 ]
    }

    [<Fact>]
    let ``TaskSeq-sortWith by string length`` () = task {
        let! result =
            taskSeq { yield! [ "banana"; "kiwi"; "cherry"; "fig" ] }
            |> TaskSeq.sortWith (fun a b -> compare a.Length b.Length)
            |> TaskSeq.toListAsync

        result
        |> List.map (fun s -> s.Length)
        |> should equal [ 3; 4; 6; 6 ]
    }
