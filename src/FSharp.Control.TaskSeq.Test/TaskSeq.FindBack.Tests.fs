module TaskSeq.Tests.FindBack

open System.Collections.Generic

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.findBack
// TaskSeq.findBackAsync
// TaskSeq.tryFindBack
// TaskSeq.tryFindBackAsync
// TaskSeq.findIndexBack
// TaskSeq.findIndexBackAsync
// TaskSeq.tryFindIndexBack
// TaskSeq.tryFindIndexBackAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.findBack (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.findBackAsync (fun _ -> Task.fromResult false) null

        assertNullArg
        <| fun () -> TaskSeq.tryFindBack (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.tryFindBackAsync (fun _ -> Task.fromResult false) null

        assertNullArg
        <| fun () -> TaskSeq.findIndexBack (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.findIndexBackAsync (fun _ -> Task.fromResult false) null

        assertNullArg
        <| fun () -> TaskSeq.tryFindIndexBack (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.tryFindIndexBackAsync (fun _ -> Task.fromResult false) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-findBack raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.findBack ((=) 12)
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-findBackAsync raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.findBackAsync (fun x -> task { return x = 12 })
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFindBack returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFindBack ((=) 12)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFindBackAsync returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFindBackAsync (fun x -> task { return x = 12 })
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-findIndexBack raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.findIndexBack ((=) 12)
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-findIndexBackAsync raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.findIndexBackAsync (fun x -> task { return x = 12 })
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFindIndexBack returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFindIndexBack ((=) 12)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFindIndexBackAsync returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFindIndexBackAsync (fun x -> task { return x = 12 })
        |> Task.map (should be None')

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findBack sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.findBack ((=) 0) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findBackAsync sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.findBackAsync (fun x -> task { return x = 0 }) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findBack happy path returns last match`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findBack (fun x -> x < 6 && x > 3) // matches 4, 5 — last is 5
        |> Task.map (should equal 5)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findBackAsync happy path returns last match`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findBackAsync (fun x -> task { return x < 6 && x > 3 }) // matches 4, 5 — last is 5
        |> Task.map (should equal 5)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findBack happy path returns last item`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findBack (fun x -> x <= 10) // all items qualify; last is 10
        |> Task.map (should equal 10)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findBackAsync happy path returns last item`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findBackAsync (fun x -> task { return x <= 10 }) // all items qualify; last is 10
        |> Task.map (should equal 10)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findBack happy path returns only matching item`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findBack ((=) 7) // exactly one match
        |> Task.map (should equal 7)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexBack sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.findIndexBack ((=) 0) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexBackAsync sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.findIndexBackAsync (fun x -> task { return x = 0 }) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexBack happy path returns last matching index`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndexBack (fun x -> x < 6 && x > 3) // matches indices 3, 4 — last is 4
        |> Task.map (should equal 4)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexBackAsync happy path returns last matching index`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndexBackAsync (fun x -> task { return x < 6 && x > 3 }) // matches indices 3, 4 — last is 4
        |> Task.map (should equal 4)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexBack happy path returns last index when all match`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndexBack (fun x -> x <= 10) // all 10 items qualify; last index is 9
        |> Task.map (should equal 9)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexBack happy path single match`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndexBack ((=) 1) // value 1 is at index 0
        |> Task.map (should equal 0)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindBack sad path returns None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindBack ((=) 0) // dummy tasks sequence starts at 1
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindBackAsync sad path returns None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindBackAsync (fun x -> task { return x = 0 }) // dummy tasks sequence starts at 1
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindBack happy path returns last match`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindBack (fun x -> x < 6 && x > 3)
        |> Task.map (should equal (Some 5))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindBackAsync happy path returns last match`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindBackAsync (fun x -> task { return x < 6 && x > 3 })
        |> Task.map (should equal (Some 5))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndexBack sad path returns None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndexBack ((=) 0)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndexBackAsync sad path returns None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndexBackAsync (fun x -> task { return x = 0 })
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndexBack happy path returns last matching index`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndexBack (fun x -> x < 6 && x > 3)
        |> Task.map (should equal (Some 4))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndexBackAsync happy path returns last matching index`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndexBackAsync (fun x -> task { return x < 6 && x > 3 })
        |> Task.map (should equal (Some 4))

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-findBack consumes the entire sequence`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.findBack (fun x -> x < 6 && x > 3)
        |> Task.map (should equal 5)

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryFindBack consumes the entire sequence`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.tryFindBack (fun x -> x < 6 && x > 3)
        |> Task.map (should equal (Some 5))

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-findIndexBack consumes the entire sequence`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.findIndexBack (fun x -> x < 6 && x > 3)
        |> Task.map (should equal 4)

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryFindIndexBack consumes the entire sequence`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.tryFindIndexBack (fun x -> x < 6 && x > 3)
        |> Task.map (should equal (Some 4))

    [<Fact>]
    let ``TaskSeq-findBack _specialcase_ unlike findBack, findBack always evaluates the full sequence`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1 // side effect after the matching yield
            yield 1 // an item after the match
        }

        // findBack must find the LAST match so it evaluates everything
        let! found = ts |> TaskSeq.findBack ((=) 42)
        found |> should equal 42
        i |> should equal 1 // side effect WAS executed — findBack consumed all items
    }

    [<Fact>]
    let ``TaskSeq-tryFindBack _specialcase_ always evaluates the full sequence`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            yield 1
        }

        let! found = ts |> TaskSeq.tryFindBack ((=) 42)
        found |> should equal (Some 42)
        i |> should equal 1 // side effect WAS executed
    }

    [<Fact>]
    let ``TaskSeq-findBack _specialcase_ returns the last of multiple matches`` () = task {
        let ts = taskSeq { yield! [ 3; 5; 3; 7; 3 ] }

        let! found = ts |> TaskSeq.findBack ((=) 3)
        found |> should equal 3 // value is 3 (last of three matches)
    }

    [<Fact>]
    let ``TaskSeq-findIndexBack _specialcase_ returns the last matching index among multiple matches`` () = task {
        let ts = taskSeq { yield! [ 3; 5; 3; 7; 3 ] }

        let! found = ts |> TaskSeq.findIndexBack ((=) 3)
        found |> should equal 4 // index 4 is the last 3
    }

    [<Fact>]
    let ``TaskSeq-tryFindBack _specialcase_ returns last match when multiple exist`` () = task {
        let ts = taskSeq { yield! [ 1; 2; 3; 4; 5; 4; 3; 2; 1 ] }

        let! found = ts |> TaskSeq.tryFindBack (fun x -> x > 3)
        found |> should equal (Some 4) // last element > 3 is the 4 at index 5
    }

    [<Fact>]
    let ``TaskSeq-tryFindIndexBack _specialcase_ returns last matching index when multiple exist`` () = task {
        let ts = taskSeq { yield! [ 1; 2; 3; 4; 5; 4; 3; 2; 1 ] }

        let! found = ts |> TaskSeq.tryFindIndexBack (fun x -> x > 3)
        found |> should equal (Some 5) // last element > 3 is at index 5
    }

    [<Fact>]
    let ``TaskSeq-findIndexBack _specialcase_ single-element sequence`` () = task {
        let ts = taskSeq { yield 42 }

        let! found = ts |> TaskSeq.findIndexBack ((=) 42)
        found |> should equal 0
    }

    [<Fact>]
    let ``TaskSeq-tryFindBack _specialcase_ single-element no match returns None`` () = task {
        let ts = taskSeq { yield 42 }

        let! found = ts |> TaskSeq.tryFindBack ((=) 99)
        found |> should be None'
    }
