module TaskSeq.Tests.Exists

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.exists
// TaskSeq.existsAsyncc
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.exists (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.existsAsync (fun _ -> Task.fromResult false) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-exists returns false`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.exists ((=) 12)
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-existsAsync returns false`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.existsAsync (fun x -> task { return x = 12 })
        |> Task.map (should be False)

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exists sad path returns false`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.exists ((=) 0)
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-existsAsync sad path return false`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.existsAsync (fun x -> task { return x = 0 })
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exists happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.exists (fun x -> x < 6 && x > 4)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-existsAsync happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.existsAsync (fun x -> task { return x < 6 && x > 4 })
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exists happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.exists ((=) 1)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-existsAsync happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.existsAsync (fun x -> task { return x = 1 })
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exists happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.exists ((=) 10)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-existsAsync happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.existsAsync (fun x -> task { return x = 10 })
        |> Task.map (should be True)

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-exists KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let finder = (=) 11

        // first: false
        let! found = TaskSeq.exists finder ts
        found |> should be False

        // find again: found now, because of side effects
        let! found = TaskSeq.exists finder ts
        found |> should be True

        // find once more: false
        let! found = TaskSeq.exists finder ts
        found |> should be False
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-existsAsync KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let finder x = task { return x = 11 }

        // first: false
        let! found = TaskSeq.existsAsync finder ts
        found |> should be False

        // find again: found now, because of side effects
        let! found = TaskSeq.existsAsync finder ts
        found |> should be True

        // find once more: false
        let! found = TaskSeq.existsAsync finder ts
        found |> should be False
    }

    [<Fact>]
    let ``TaskSeq-exists _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.exists ((=) 3)
        found |> should be True
        i |> should equal 3 // only partial evaluation!

        // find next item. We do get a new iterator, but mutable state is now starting at '3', so first item now returned is '4'.
        let! found = ts |> TaskSeq.exists ((=) 4)
        found |> should be True
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-existsAsync _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.existsAsync (fun x -> task { return x = 3 })
        found |> should be True
        i |> should equal 3 // only partial evaluation!

        // find next item. We do get a new iterator, but mutable state is now starting at '3', so first item now returned is '4'.
        let! found = ts |> TaskSeq.existsAsync (fun x -> task { return x = 4 })
        found |> should be True
        i |> should equal 4
    }

    [<Fact>]
    let ``TaskSeq-exists _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.exists ((=) 42)
        found |> should be True
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-existsAsync _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.existsAsync (fun x -> task { return x = 42 })
        found |> should be True
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-exists _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.exists ((=) 0)
        found |> should be True
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // find some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.exists ((=) 4)
        found |> should be True
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-existsAsync _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.existsAsync (fun x -> task { return x = 0 })
        found |> should be True
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // find some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.existsAsync (fun x -> task { return x = 4 })
        found |> should be True
        i |> should equal 4 // only partial evaluation!
    }
