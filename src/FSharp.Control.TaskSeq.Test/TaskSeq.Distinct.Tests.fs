module TaskSeq.Tests.Distinct

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.distinct
// TaskSeq.distinctBy
// TaskSeq.distinctByAsync
//


module EmptySeq =
    [<Fact>]
    let ``TaskSeq-distinct with null source raises`` () = assertNullArg <| fun () -> TaskSeq.distinct null

    [<Fact>]
    let ``TaskSeq-distinctBy with null source raises`` () = assertNullArg <| fun () -> TaskSeq.distinctBy id null

    [<Fact>]
    let ``TaskSeq-distinctByAsync with null source raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.distinctByAsync (fun x -> Task.fromResult x) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-distinct on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.distinct
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-distinctBy on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.distinctBy id
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-distinctByAsync on empty returns empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.distinctByAsync (fun x -> Task.fromResult x)
        |> verifyEmpty


module Functionality =
    [<Fact>]
    let ``TaskSeq-distinct removes duplicate ints`` () = task {
        let! result =
            taskSeq { yield! [ 1; 2; 2; 3; 1; 4; 3; 5 ] }
            |> TaskSeq.distinct
            |> TaskSeq.toListAsync

        result |> should equal [ 1; 2; 3; 4; 5 ]
    }

    [<Fact>]
    let ``TaskSeq-distinct removes duplicate strings`` () = task {
        let! result =
            taskSeq { yield! [ "a"; "b"; "b"; "a"; "c" ] }
            |> TaskSeq.distinct
            |> TaskSeq.toListAsync

        result |> should equal [ "a"; "b"; "c" ]
    }

    [<Fact>]
    let ``TaskSeq-distinct with all identical elements returns singleton`` () = task {
        let! result =
            taskSeq { yield! [ 7; 7; 7; 7; 7 ] }
            |> TaskSeq.distinct
            |> TaskSeq.toListAsync

        result |> should equal [ 7 ]
    }

    [<Fact>]
    let ``TaskSeq-distinct with all distinct elements returns all`` () = task {
        let! result =
            taskSeq { yield! [ 1..5 ] }
            |> TaskSeq.distinct
            |> TaskSeq.toListAsync

        result |> should equal [ 1; 2; 3; 4; 5 ]
    }

    [<Fact>]
    let ``TaskSeq-distinct on singleton returns singleton`` () = task {
        let! result =
            taskSeq { yield 42 }
            |> TaskSeq.distinct
            |> TaskSeq.toListAsync

        result |> should equal [ 42 ]
    }

    [<Fact>]
    let ``TaskSeq-distinct keeps first occurrence, not last`` () = task {
        // sequence [3;1;2;1;3] - first occurrences are at indices 0,1,2 for values 3,1,2
        let! result =
            taskSeq { yield! [ 3; 1; 2; 1; 3 ] }
            |> TaskSeq.distinct
            |> TaskSeq.toListAsync

        result |> should equal [ 3; 1; 2 ]
    }

    [<Fact>]
    let ``TaskSeq-distinct is different from distinctUntilChanged`` () = task {
        // [1;2;1] - distinct gives [1;2], distinctUntilChanged gives [1;2;1]
        let! distinct =
            taskSeq { yield! [ 1; 2; 1 ] }
            |> TaskSeq.distinct
            |> TaskSeq.toListAsync

        let! distinctUntilChanged =
            taskSeq { yield! [ 1; 2; 1 ] }
            |> TaskSeq.distinctUntilChanged
            |> TaskSeq.toListAsync

        distinct |> should equal [ 1; 2 ]
        distinctUntilChanged |> should equal [ 1; 2; 1 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctBy removes elements with duplicate projected keys`` () = task {
        let! result =
            taskSeq { yield! [ 1; 2; 3; 4; 5; 6 ] }
            |> TaskSeq.distinctBy (fun x -> x % 3)
            |> TaskSeq.toListAsync

        // keys: 1%3=1, 2%3=2, 3%3=0, 4%3=1(dup), 5%3=2(dup), 6%3=0(dup)
        result |> should equal [ 1; 2; 3 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctBy with string length as key`` () = task {
        let! result =
            taskSeq { yield! [ "a"; "bb"; "c"; "dd"; "eee" ] }
            |> TaskSeq.distinctBy String.length
            |> TaskSeq.toListAsync

        // lengths: 1, 2, 1(dup), 2(dup), 3
        result |> should equal [ "a"; "bb"; "eee" ]
    }

    [<Fact>]
    let ``TaskSeq-distinctBy with identity projection equals distinct`` () = task {
        let input = [ 1; 2; 2; 3; 1; 4 ]

        let! byId =
            taskSeq { yield! input }
            |> TaskSeq.distinctBy id
            |> TaskSeq.toListAsync

        let! plain =
            taskSeq { yield! input }
            |> TaskSeq.distinct
            |> TaskSeq.toListAsync

        byId |> should equal plain
    }

    [<Fact>]
    let ``TaskSeq-distinctBy keeps first element with a given key`` () = task {
        let! result =
            taskSeq { yield! [ (1, "a"); (2, "b"); (1, "c") ] }
            |> TaskSeq.distinctBy fst
            |> TaskSeq.toListAsync

        result |> should equal [ (1, "a"); (2, "b") ]
    }

    [<Fact>]
    let ``TaskSeq-distinctByAsync removes elements with duplicate projected keys`` () = task {
        let! result =
            taskSeq { yield! [ 1; 2; 3; 4; 5; 6 ] }
            |> TaskSeq.distinctByAsync (fun x -> task { return x % 3 })
            |> TaskSeq.toListAsync

        result |> should equal [ 1; 2; 3 ]
    }

    [<Fact>]
    let ``TaskSeq-distinctByAsync behaves identically to distinctBy`` () = task {
        let input = [ 1; 2; 2; 3; 1; 4 ]
        let projection x = x % 2

        let! bySync =
            taskSeq { yield! input }
            |> TaskSeq.distinctBy projection
            |> TaskSeq.toListAsync

        let! byAsync =
            taskSeq { yield! input }
            |> TaskSeq.distinctByAsync (fun x -> task { return projection x })
            |> TaskSeq.toListAsync

        bySync |> should equal byAsync
    }

    [<Fact>]
    let ``TaskSeq-distinct with chars`` () = task {
        let! result =
            taskSeq { yield! [ 'A'; 'A'; 'B'; 'Z'; 'C'; 'C'; 'Z'; 'C'; 'D'; 'D'; 'D'; 'Z' ] }
            |> TaskSeq.distinct
            |> TaskSeq.toListAsync

        result |> should equal [ 'A'; 'B'; 'Z'; 'C'; 'D' ]
    }


module SideEffects =
    [<Fact>]
    let ``TaskSeq-distinct evaluates elements lazily`` () = task {
        let mutable sideEffects = 0

        let ts = taskSeq {
            for i in 1..5 do
                sideEffects <- sideEffects + 1
                yield i
        }

        let distinct = ts |> TaskSeq.distinct

        // no evaluation yet
        sideEffects |> should equal 0

        let! _ = distinct |> TaskSeq.toListAsync

        // only evaluated when consumed
        sideEffects |> should equal 5
    }

    [<Fact>]
    let ``TaskSeq-distinctBy evaluates projection lazily`` () = task {
        let mutable projections = 0

        let! result =
            taskSeq { yield! [ 1; 2; 3; 1; 2 ] }
            |> TaskSeq.distinctBy (fun x ->
                projections <- projections + 1
                x)
            |> TaskSeq.toListAsync

        result |> should equal [ 1; 2; 3 ]
        // projection called once per element (5 elements)
        projections |> should equal 5
    }
