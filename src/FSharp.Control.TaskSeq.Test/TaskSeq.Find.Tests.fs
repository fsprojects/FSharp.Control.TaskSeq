module TaskSeq.Tests.Find

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control
open System.Collections.Generic

//
// TaskSeq.find
// TaskSeq.findAsync
// TaskSeq.tryFind
// TaskSeq.tryFindAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.find (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.findAsync (fun _ -> Task.fromResult false) null

        assertNullArg
        <| fun () -> TaskSeq.tryFind (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.tryFindAsync (fun _ -> Task.fromResult false) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-find raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.find ((=) 12)
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-findAsync raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.findAsync (fun x -> task { return x = 12 })
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>


    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFind returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFind ((=) 12)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFindAsync returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x = 12 })
        |> Task.map (should be None')

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-find sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.find ((=) 0) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findAsync sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.findAsync (fun x -> task { return x = 0 }) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-find happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.find (fun x -> x < 6 && x > 4)
        |> Task.map (should equal 5)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findAsync happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findAsync (fun x -> task { return x < 6 && x > 4 })
        |> Task.map (should equal 5)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-find happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.find ((=) 1)
        |> Task.map (should equal 1)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findAsync happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findAsync (fun x -> task { return x = 1 })
        |> Task.map (should equal 1)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-find happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.find ((=) 10)
        |> Task.map (should equal 10)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findAsync happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findAsync (fun x -> task { return x = 10 }) // dummy tasks seq ends at 50
        |> Task.map (should equal 10)


    //
    //
    // tryXXX stuff
    //      |
    //      |
    //      V

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFind sad path returns None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFind ((=) 0)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindAsync sad path return None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x = 0 })
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFind happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFind (fun x -> x < 6 && x > 4)
        |> Task.map (should equal (Some 5))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindAsync happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x < 6 && x > 4 })
        |> Task.map (should equal (Some 5))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFind happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFind ((=) 1)
        |> Task.map (should equal (Some 1))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindAsync happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x = 1 })
        |> Task.map (should equal (Some 1))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFind happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFind ((=) 10)
        |> Task.map (should equal (Some 10))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindAsync happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x = 10 })
        |> Task.map (should equal (Some 10))

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-find KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let finder = (=) 11

        // first: error, item is not there
        fun () -> TaskSeq.find finder ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

        // find again: no error, because of side effects
        let! found = TaskSeq.find finder ts
        found |> should equal 11

        // find once more: error, item is not there anymore.
        fun () -> TaskSeq.find finder ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-findAsync KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let finder x = task { return x = 11 }

        // first: error, item is not there
        fun () -> TaskSeq.findAsync finder ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

        // find again: no error, because of side effects
        let! found = TaskSeq.findAsync finder ts
        found |> should equal 11

        // find once more: error, item is not there anymore.
        fun () -> TaskSeq.findAsync finder ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Fact>]
    let ``TaskSeq-find _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.find ((=) 3)
        found |> should equal 3
        i |> should equal 3 // only partial evaluation!

        // find next item. We do get a new iterator, but mutable state is now starting at '3', so first item now returned is '4'.
        let! found = ts |> TaskSeq.find ((=) 4)
        found |> should equal 4
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-findAsync _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.findAsync (fun x -> task { return x = 3 })
        found |> should equal 3
        i |> should equal 3 // only partial evaluation!

        // find next item. We do get a new iterator, but mutable state is now starting at '3', so first item now returned is '4'.
        let! found = ts |> TaskSeq.findAsync (fun x -> task { return x = 4 })
        found |> should equal 4
        i |> should equal 4
    }

    [<Fact>]
    let ``TaskSeq-find _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.find ((=) 42)
        found |> should equal 42
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-findAsync _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.findAsync (fun x -> task { return x = 42 })
        found |> should equal 42
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-find _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.find ((=) 0)
        found |> should equal 0
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // find some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.find ((=) 4)
        found |> should equal 4
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-findAsync _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.findAsync (fun x -> task { return x = 0 })
        found |> should equal 0
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // find some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.findAsync (fun x -> task { return x = 4 })
        found |> should equal 4
        i |> should equal 4 // only partial evaluation!
    }


    //
    //
    // tryXXX stuff
    //      |
    //      |
    //      V

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryFind KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let finder = (=) 11

        // first: None
        let! found = TaskSeq.tryFind finder ts
        found |> should be None'

        // find again: found now, because of side effects
        let! found = TaskSeq.tryFind finder ts
        found |> should equal (Some 11)

        // find once more: None
        let! found = TaskSeq.tryFind finder ts
        found |> should be None'
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryFindAsync KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let finder x = task { return x = 11 }

        // first: None
        let! found = TaskSeq.tryFindAsync finder ts
        found |> should be None'

        // find again: found now, because of side effects
        let! found = TaskSeq.tryFindAsync finder ts
        found |> should equal (Some 11)

        // find once more: None
        let! found = TaskSeq.tryFindAsync finder ts
        found |> should be None'
    }

    [<Fact>]
    let ``TaskSeq-tryFind _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.tryFind ((=) 3)
        found |> should equal (Some 3)
        i |> should equal 3 // only partial evaluation!

        // find next item. We do get a new iterator, but mutable state is now starting at '3', so first item now returned is '4'.
        let! found = ts |> TaskSeq.tryFind ((=) 4)
        found |> should equal (Some 4)
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-tryFindAsync _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.tryFindAsync (fun x -> task { return x = 3 })
        found |> should equal (Some 3)
        i |> should equal 3 // only partial evaluation!

        // find next item. We do get a new iterator, but mutable state is now starting at '3', so first item now returned is '4'.
        let! found = ts |> TaskSeq.tryFindAsync (fun x -> task { return x = 4 })
        found |> should equal (Some 4)
        i |> should equal 4
    }

    [<Fact>]
    let ``TaskSeq-tryFind _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.tryFind ((=) 42)
        found |> should equal (Some 42)
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-tryFindAsync _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.tryFindAsync (fun x -> task { return x = 42 })
        found |> should equal (Some 42)
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-tryFind _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.tryFind ((=) 0)
        found |> should equal (Some 0)
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // find some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.tryFind ((=) 4)
        found |> should equal (Some 4)
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-tryFindAsync _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.tryFindAsync (fun x -> task { return x = 0 })
        found |> should equal (Some 0)
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // find some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.tryFindAsync (fun x -> task { return x = 4 })
        found |> should equal (Some 4)
        i |> should equal 4 // only partial evaluation!
    }
