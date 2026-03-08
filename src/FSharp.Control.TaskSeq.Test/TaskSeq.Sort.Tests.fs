module TaskSeq.Tests.Sort

open System.Threading.Tasks
open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.rev / TaskSeq.sort / TaskSeq.sortDescending
// TaskSeq.sortBy / TaskSeq.sortByAsync
// TaskSeq.sortByDescending / TaskSeq.sortByDescendingAsync
// TaskSeq.sortWith
//

module Rev =
    module EmptySeq =
        [<Fact>]
        let ``TaskSeq-rev with null source raises`` () = assertNullArg <| fun () -> TaskSeq.rev null

        [<Theory; ClassData(typeof<TestEmptyVariants>)>]
        let ``TaskSeq-rev on empty returns empty`` variant = Gen.getEmptyVariant variant |> TaskSeq.rev |> verifyEmpty

    module Immutable =
        [<Fact>]
        let ``TaskSeq-rev on single element returns same element`` () = task {
            let! result = taskSeq { yield 42 } |> TaskSeq.rev |> TaskSeq.toListAsync
            result |> should equal [ 42 ]
        }

        [<Fact>]
        let ``TaskSeq-rev reverses ascending sequence`` () = task {
            let! result =
                taskSeq { yield! [ 1..5 ] }
                |> TaskSeq.rev
                |> TaskSeq.toListAsync

            result |> should equal [ 5; 4; 3; 2; 1 ]
        }

        [<Fact>]
        let ``TaskSeq-rev of rev is identity`` () = task {
            let! result =
                taskSeq { yield! [ 10; 20; 30 ] }
                |> TaskSeq.rev
                |> TaskSeq.rev
                |> TaskSeq.toListAsync

            result |> should equal [ 10; 20; 30 ]
        }

        [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
        let ``TaskSeq-rev all variants - correct length and reversed order`` variant = task {
            let! result =
                Gen.getSeqImmutable variant
                |> TaskSeq.rev
                |> TaskSeq.toListAsync

            result |> List.length |> should equal 10
            result |> List.head |> should equal 10
            result |> List.last |> should equal 1
        }

module Sort =
    module EmptySeq =
        [<Fact>]
        let ``TaskSeq-sort with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sort null

        [<Theory; ClassData(typeof<TestEmptyVariants>)>]
        let ``TaskSeq-sort on empty returns empty`` variant = Gen.getEmptyVariant variant |> TaskSeq.sort |> verifyEmpty

    module Immutable =
        [<Fact>]
        let ``TaskSeq-sort on single element returns same element`` () = task {
            let! result = taskSeq { yield 42 } |> TaskSeq.sort |> TaskSeq.toListAsync
            result |> should equal [ 42 ]
        }

        [<Fact>]
        let ``TaskSeq-sort sorts integers ascending`` () = task {
            let! result =
                taskSeq { yield! [ 3; 1; 4; 1; 5; 9; 2; 6 ] }
                |> TaskSeq.sort
                |> TaskSeq.toListAsync

            result |> should equal [ 1; 1; 2; 3; 4; 5; 6; 9 ]
        }

        [<Fact>]
        let ``TaskSeq-sort sorts strings lexicographically`` () = task {
            let! result =
                taskSeq { yield! [ "banana"; "apple"; "cherry" ] }
                |> TaskSeq.sort
                |> TaskSeq.toListAsync

            result |> should equal [ "apple"; "banana"; "cherry" ]
        }

        [<Fact>]
        let ``TaskSeq-sort already-sorted sequence stays sorted`` () = task {
            let! result =
                taskSeq { yield! [ 1..10 ] }
                |> TaskSeq.sort
                |> TaskSeq.toListAsync

            result |> should equal [ 1..10 ]
        }

module SortDescending =
    module EmptySeq =
        [<Fact>]
        let ``TaskSeq-sortDescending with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sortDescending null

        [<Theory; ClassData(typeof<TestEmptyVariants>)>]
        let ``TaskSeq-sortDescending on empty returns empty`` variant =
            Gen.getEmptyVariant variant
            |> TaskSeq.sortDescending
            |> verifyEmpty

    module Immutable =
        [<Fact>]
        let ``TaskSeq-sortDescending sorts integers descending`` () = task {
            let! result =
                taskSeq { yield! [ 3; 1; 4; 1; 5; 9; 2; 6 ] }
                |> TaskSeq.sortDescending
                |> TaskSeq.toListAsync

            result |> should equal [ 9; 6; 5; 4; 3; 2; 1; 1 ]
        }

        [<Fact>]
        let ``TaskSeq-sortDescending is reverse of sort`` () = task {
            let input = [ 3; 1; 4; 1; 5; 9; 2; 6 ]

            let! ascending =
                taskSeq { yield! input }
                |> TaskSeq.sort
                |> TaskSeq.toListAsync

            let! descending =
                taskSeq { yield! input }
                |> TaskSeq.sortDescending
                |> TaskSeq.toListAsync

            descending |> should equal (List.rev ascending)
        }

module SortBy =
    module EmptySeq =
        [<Fact>]
        let ``TaskSeq-sortBy with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sortBy id null

        [<Theory; ClassData(typeof<TestEmptyVariants>)>]
        let ``TaskSeq-sortBy on empty returns empty`` variant =
            Gen.getEmptyVariant variant
            |> TaskSeq.sortBy id
            |> verifyEmpty

    module Immutable =
        [<Fact>]
        let ``TaskSeq-sortBy sorts by string length`` () = task {
            let! result =
                taskSeq { yield! [ "banana"; "fig"; "apple"; "kiwi" ] }
                |> TaskSeq.sortBy String.length
                |> TaskSeq.toListAsync

            result
            |> List.map String.length
            |> should equal [ 3; 4; 5; 6 ]
        }

        [<Fact>]
        let ``TaskSeq-sortBy sorts tuples by key`` () = task {
            let! result =
                taskSeq { yield! [ ("b", 2); ("a", 1); ("c", 3) ] }
                |> TaskSeq.sortBy fst
                |> TaskSeq.toListAsync

            result |> List.map fst |> should equal [ "a"; "b"; "c" ]
        }

        [<Fact>]
        let ``TaskSeq-sortBy with identity is equivalent to sort`` () = task {
            let input = [ 5; 3; 8; 1; 4 ]

            let! sorted =
                taskSeq { yield! input }
                |> TaskSeq.sort
                |> TaskSeq.toListAsync

            let! sortedById =
                taskSeq { yield! input }
                |> TaskSeq.sortBy id
                |> TaskSeq.toListAsync

            sortedById |> should equal sorted
        }

module SortByDescending =
    module EmptySeq =
        [<Fact>]
        let ``TaskSeq-sortByDescending with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sortByDescending id null

        [<Theory; ClassData(typeof<TestEmptyVariants>)>]
        let ``TaskSeq-sortByDescending on empty returns empty`` variant =
            Gen.getEmptyVariant variant
            |> TaskSeq.sortByDescending id
            |> verifyEmpty

    module Immutable =
        [<Fact>]
        let ``TaskSeq-sortByDescending sorts by string length descending`` () = task {
            let! result =
                taskSeq { yield! [ "banana"; "fig"; "apple"; "kiwi" ] }
                |> TaskSeq.sortByDescending String.length
                |> TaskSeq.toListAsync

            result
            |> List.map String.length
            |> should equal [ 6; 5; 4; 3 ]
        }

        [<Fact>]
        let ``TaskSeq-sortByDescending is reverse of sortBy for same projection`` () = task {
            let input = [ "banana"; "fig"; "apple"; "kiwi" ]

            let! ascending =
                taskSeq { yield! input }
                |> TaskSeq.sortBy String.length
                |> TaskSeq.toListAsync

            let! descending =
                taskSeq { yield! input }
                |> TaskSeq.sortByDescending String.length
                |> TaskSeq.toListAsync

            descending |> should equal (List.rev ascending)
        }

module SortByAsync =
    module EmptySeq =
        [<Fact>]
        let ``TaskSeq-sortByAsync with null source raises`` () =
            assertNullArg
            <| fun () -> TaskSeq.sortByAsync (fun x -> Task.FromResult x) null

        [<Theory; ClassData(typeof<TestEmptyVariants>)>]
        let ``TaskSeq-sortByAsync on empty returns empty`` variant = task {
            do!
                Gen.getEmptyVariant variant
                |> TaskSeq.sortByAsync (fun x -> Task.FromResult x)
                |> verifyEmpty
        }

    module Immutable =
        [<Fact>]
        let ``TaskSeq-sortByAsync sorts by async key`` () = task {
            let! result =
                taskSeq { yield! [ "banana"; "fig"; "apple"; "kiwi" ] }
                |> TaskSeq.sortByAsync (fun s -> Task.FromResult(String.length s))
                |> TaskSeq.toListAsync

            result
            |> List.map String.length
            |> should equal [ 3; 4; 5; 6 ]
        }

        [<Fact>]
        let ``TaskSeq-sortByAsync result matches synchronous sortBy`` () = task {
            let input = [ 5; 3; 8; 1; 4 ]

            let! syncResult =
                taskSeq { yield! input }
                |> TaskSeq.sortBy id
                |> TaskSeq.toListAsync

            let! asyncResult =
                taskSeq { yield! input }
                |> TaskSeq.sortByAsync (fun x -> Task.FromResult x)
                |> TaskSeq.toListAsync

            asyncResult |> should equal syncResult
        }

module SortByDescendingAsync =
    module EmptySeq =
        [<Fact>]
        let ``TaskSeq-sortByDescendingAsync with null source raises`` () =
            assertNullArg
            <| fun () -> TaskSeq.sortByDescendingAsync (fun x -> Task.FromResult x) null

        [<Theory; ClassData(typeof<TestEmptyVariants>)>]
        let ``TaskSeq-sortByDescendingAsync on empty returns empty`` variant = task {
            do!
                Gen.getEmptyVariant variant
                |> TaskSeq.sortByDescendingAsync (fun x -> Task.FromResult x)
                |> verifyEmpty
        }

    module Immutable =
        [<Fact>]
        let ``TaskSeq-sortByDescendingAsync sorts descending by async key`` () = task {
            let! result =
                taskSeq { yield! [ "banana"; "fig"; "apple"; "kiwi" ] }
                |> TaskSeq.sortByDescendingAsync (fun s -> Task.FromResult(String.length s))
                |> TaskSeq.toListAsync

            result
            |> List.map String.length
            |> should equal [ 6; 5; 4; 3 ]
        }

        [<Fact>]
        let ``TaskSeq-sortByDescendingAsync result matches sortByDescending`` () = task {
            let input = [ 5; 3; 8; 1; 4 ]

            let! syncResult =
                taskSeq { yield! input }
                |> TaskSeq.sortByDescending id
                |> TaskSeq.toListAsync

            let! asyncResult =
                taskSeq { yield! input }
                |> TaskSeq.sortByDescendingAsync (fun x -> Task.FromResult x)
                |> TaskSeq.toListAsync

            asyncResult |> should equal syncResult
        }

module SortWith =
    module EmptySeq =
        [<Fact>]
        let ``TaskSeq-sortWith with null source raises`` () = assertNullArg <| fun () -> TaskSeq.sortWith compare null

        [<Theory; ClassData(typeof<TestEmptyVariants>)>]
        let ``TaskSeq-sortWith on empty returns empty`` variant =
            Gen.getEmptyVariant variant
            |> TaskSeq.sortWith compare
            |> verifyEmpty

    module Immutable =
        [<Fact>]
        let ``TaskSeq-sortWith with standard compare gives ascending order`` () = task {
            let! result =
                taskSeq { yield! [ 3; 1; 4; 1; 5; 9; 2; 6 ] }
                |> TaskSeq.sortWith compare
                |> TaskSeq.toListAsync

            result |> should equal [ 1; 1; 2; 3; 4; 5; 6; 9 ]
        }

        [<Fact>]
        let ``TaskSeq-sortWith with reversed compare gives descending order`` () = task {
            let! result =
                taskSeq { yield! [ 3; 1; 4; 1; 5; 9; 2; 6 ] }
                |> TaskSeq.sortWith (fun a b -> compare b a)
                |> TaskSeq.toListAsync

            result |> should equal [ 9; 6; 5; 4; 3; 2; 1; 1 ]
        }

        [<Fact>]
        let ``TaskSeq-sortWith with custom comparer sorts by absolute value`` () = task {
            let! result =
                taskSeq { yield! [ -3; 1; -4; 2; -5 ] }
                |> TaskSeq.sortWith (fun a b -> compare (abs a) (abs b))
                |> TaskSeq.toListAsync

            result |> List.map abs |> should equal [ 1; 2; 3; 4; 5 ]
        }
