module TaskSeq.Tests.FindIndex

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control
open System.Collections.Generic

//
// TaskSeq.findIndex
// TaskSeq.findIndexAsync
// TaskSeq.tryFindIndex
// TaskSeq.tryFindIndexAsync
//

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-findIndex raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.findIndex ((=) 12)
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-findIndexAsync raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.findIndexAsync (fun x -> task { return x = 12 })
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>


    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFindIndex returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFindIndex ((=) 12)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFindIndexAsync returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFindIndexAsync (fun x -> task { return x = 12 })
        |> Task.map (should be None')

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndex sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.findIndex ((=) 0) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexAsync sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.findIndexAsync (fun x -> task { return x = 0 }) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndex happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndex (fun x -> x < 6 && x > 4)
        |> Task.map (should equal 4) // zero based

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexAsync happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndexAsync (fun x -> task { return x < 6 && x > 4 })
        |> Task.map (should equal 4)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndex happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndex ((=) 1)
        |> Task.map (should equal 0) // zero based

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexAsync happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndexAsync (fun x -> task { return x = 1 })
        |> Task.map (should equal 0) // zero based

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndex happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndex ((=) 10)
        |> Task.map (should equal 9) // zero based

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findIndexAsync happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findIndexAsync (fun x -> task { return x = 10 }) // dummy tasks seq ends at 50
        |> Task.map (should equal 9) // zero based


    //
    //
    // tryXXX stuff
    //      |
    //      |
    //      V

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndex sad path returns None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndex ((=) 0)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndexAsync sad path return None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndexAsync (fun x -> task { return x = 0 })
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndex happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndex (fun x -> x < 6 && x > 4)
        |> Task.map (should equal (Some 4)) // zero based

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndexAsync happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndexAsync (fun x -> task { return x < 6 && x > 4 })
        |> Task.map (should equal (Some 4)) // zero based

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndex happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndex ((=) 1)
        |> Task.map (should equal (Some 0)) // zero based

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndexAsync happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndexAsync (fun x -> task { return x = 1 })
        |> Task.map (should equal (Some 0)) // zero based

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndex happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndex ((=) 10)
        |> Task.map (should equal (Some 9)) // zero based

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindIndexAsync happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindIndexAsync (fun x -> task { return x = 10 })
        |> Task.map (should equal (Some 9)) // zero based

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-findIndex KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let findIndexer = (=) 11

        // first: error, item is not there
        fun () -> TaskSeq.findIndex findIndexer ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

        // findIndex again: no error, because of side effects
        let! found = TaskSeq.findIndex findIndexer ts
        found |> should equal 0 // zero based, first item in 'updated' sequence is 11

        // findIndex once more: error, item is not there anymore.
        fun () -> TaskSeq.findIndex findIndexer ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-findIndexAsync KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let findIndexer x = task { return x = 11 }

        // first: error, item is not there
        fun () -> TaskSeq.findIndexAsync findIndexer ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

        // findIndex again: no error, because of side effects
        let! found = TaskSeq.findIndexAsync findIndexer ts
        found |> should equal 0 // zero based, first item in 'updated' sequence is 11

        // findIndex once more: error, item is not there anymore.
        fun () -> TaskSeq.findIndexAsync findIndexer ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Fact>]
    let ``TaskSeq-findIndex _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for x in 10..19 do
                i <- i + 1
                yield x
        }

        let! found = ts |> TaskSeq.findIndex ((=) 13)
        found |> should equal 3
        i |> should equal 4 // only partial evaluation!

        // findIndex next item. We do get a new iterator, but mutable state is now starting at '4'
        let! found = ts |> TaskSeq.findIndex ((=) 14)
        found |> should equal 4
        i |> should equal 9 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-findIndexAsync _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for x in 10..19 do
                i <- i + 1
                yield x
        }

        let! found = TaskSeq.findIndexAsync (fun x -> task { return x = 13 }) ts
        found |> should equal 3
        i |> should equal 4 // only partial evaluation!

        // findIndex next item. We do get a new iterator, but mutable state is now starting at '4'
        let! found = TaskSeq.findIndexAsync (fun x -> task { return x = 14 }) ts
        found |> should equal 4
        i |> should equal 9 // started counting again
    }

    [<Fact>]
    let ``TaskSeq-findIndex _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.findIndex ((=) 42)
        found |> should equal 0 // first item has index 0
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-findIndexAsync _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = TaskSeq.findIndexAsync (fun x -> task { return x = 42 }) ts
        found |> should equal 0 // first item has index 0
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-findIndex _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for x in 10..19 do
                yield x
                i <- i + 1
        }

        let! found = ts |> TaskSeq.findIndex ((=) 10)
        found |> should equal 0
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // findIndex some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.findIndex ((=) 14)
        found |> should equal 4
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-findIndexAsync _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for x in 10..19 do
                yield x
                i <- i + 1
        }

        let! found =
            ts
            |> TaskSeq.findIndexAsync (fun x -> task { return x = 10 })

        found |> should equal 0
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // findIndex some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found =
            ts
            |> TaskSeq.findIndexAsync (fun x -> task { return x = 14 })

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
    let ``TaskSeq-tryFindIndex KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let findIndexer = (=) 11

        // first: None
        let! found = TaskSeq.tryFindIndex findIndexer ts
        found |> should be None'

        // findIndex again: found now, because of side effects
        let! found = TaskSeq.tryFindIndex findIndexer ts
        found |> should equal (Some 0) // item with value '11' is at index 0 in 'updated' sequence

        // findIndex once more: None
        let! found = TaskSeq.tryFindIndex findIndexer ts
        found |> should be None'
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryFindIndexAsync KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let findIndexer x = task { return x = 11 }

        // first: None
        let! found = TaskSeq.tryFindIndexAsync findIndexer ts
        found |> should be None'

        // findIndex again: found now, because of side effects
        let! found = TaskSeq.tryFindIndexAsync findIndexer ts
        found |> should equal (Some 0) // item with value '11' is at index 0 in 'updated' sequence

        // findIndex once more: None
        let! found = TaskSeq.tryFindIndexAsync findIndexer ts
        found |> should be None'
    }

    [<Fact>]
    let ``TaskSeq-tryFindIndex _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for x in 10..19 do
                i <- i + 1
                yield x
        }

        let! found = ts |> TaskSeq.tryFindIndex ((=) 13)
        found |> should equal (Some 3)
        i |> should equal 4 // only partial evaluation!

        // findIndex next item. We do get a new iterator, but mutable state is now starting at '4'
        let! found = ts |> TaskSeq.tryFindIndex ((=) 14)
        found |> should equal (Some 4)
        i |> should equal 9 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-tryFindIndexAsync _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for x in 10..19 do
                i <- i + 1
                yield x
        }

        let! found = TaskSeq.tryFindIndexAsync (fun x -> task { return x = 13 }) ts

        found |> should equal (Some 3)
        i |> should equal 4 // only partial evaluation!

        // findIndex next item. We do get a new iterator, but mutable state is now starting at '4'
        let! found = TaskSeq.tryFindIndexAsync (fun x -> task { return x = 14 }) ts

        found |> should equal (Some 4)
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-tryFindIndex _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.tryFindIndex ((=) 42)
        found |> should equal (Some 0) // first item has index 0
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-tryFindIndexAsync _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found =
            ts
            |> TaskSeq.tryFindIndexAsync (fun x -> task { return x = 42 })

        found |> should equal (Some 0) // first item: idx 0
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-tryFindIndex _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for x in 10..19 do
                yield x
                i <- i + 1
        }

        let! found = ts |> TaskSeq.tryFindIndex ((=) 10)
        found |> should equal (Some 0)
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // findIndex some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.tryFindIndex ((=) 14)
        found |> should equal (Some 4)
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-tryFindIndexAsync _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for x in 10..19 do
                yield x
                i <- i + 1
        }

        let! found =
            ts
            |> TaskSeq.tryFindIndexAsync (fun x -> task { return x = 10 })

        found |> should equal (Some 0)
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // findIndex some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found =
            ts
            |> TaskSeq.tryFindIndexAsync (fun x -> task { return x = 14 })

        found |> should equal (Some 4)
        i |> should equal 4 // only partial evaluation!
    }
