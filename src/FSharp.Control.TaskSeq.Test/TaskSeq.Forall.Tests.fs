module TaskSeq.Tests.Forall

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.forall
// TaskSeq.forallAsyncc
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.forall (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.forallAsync (fun _ -> Task.fromResult false) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-forall always returns true`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.forall ((=) 12)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-forallAsync always returns true`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.forallAsync (fun x -> task { return x = 12 })
        |> Task.map (should be True)

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-forall sad path returns false`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.forall ((=) 0)
            |> Task.map (should be False)

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.forall ((>) 9) // lt
            |> Task.map (should be False)
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-forallAsync sad path returns false`` variant = task {
        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.forallAsync (fun x -> task { return x = 0 })
            |> Task.map (should be False)

        do!
            Gen.getSeqImmutable variant
            |> TaskSeq.forallAsync (fun x -> task { return x < 9 })
            |> Task.map (should be False)
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-forall happy path whole seq true`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.forall (fun x -> x < 6 || x > 5)
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-forallAsync happy path whole seq true`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.forallAsync (fun x -> task { return x <= 10 && x >= 0 })
        |> Task.map (should be True)

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-forall mutated state can change result`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let predicate x = x > 10

        // first: false
        let! found = TaskSeq.forall predicate ts
        found |> should be False // fails on first item, not many side effects yet

        // ensure side effects executes
        do! consumeTaskSeq ts

        // find again: found now, because of side effects
        let! found = TaskSeq.forall predicate ts
        found |> should be True

        // find once more, still true, as numbers increase
        do! consumeTaskSeq ts // ensure side effects executes
        let! found = TaskSeq.forall predicate ts
        found |> should be True
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-forallAsync mutated state can change result`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let predicate x = Task.fromResult (x > 10)

        // first: false
        let! found = TaskSeq.forallAsync predicate ts
        found |> should be False // fails on first item, not many side effects yet

        // ensure side effects executes
        do! consumeTaskSeq ts

        // find again: found now, because of side effects
        let! found = TaskSeq.forallAsync predicate ts
        found |> should be True

        // find once more, still true, as numbers increase
        do! consumeTaskSeq ts // ensure side effects executes
        let! found = TaskSeq.forallAsync predicate ts
        found |> should be True
    }

    [<Fact>]
    let ``TaskSeq-forall _specialcase_ prove we don't read past the first failing item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.forall ((>) 3)
        found |> should be False
        i |> should equal 3 // only partial evaluation!

        // find next item. We do get a new iterator, but mutable state is now starting at '3', so first item now returned is '4'.
        let! found = ts |> TaskSeq.forall ((<=) 4)
        found |> should be True
        i |> should equal 13 // we evaluated to the end
    }

    [<Fact>]
    let ``TaskSeq-forallAsync _specialcase_ prove we don't read past the first failing item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.forallAsync (fun x -> Task.fromResult (x < 3))
        found |> should be False
        i |> should equal 3 // only partial evaluation!

        // find next item. We do get a new iterator, but mutable state is now starting at '3', so first item now returned is '4'.
        let! found =
            ts
            |> TaskSeq.forallAsync (fun x -> Task.fromResult (x >= 4))

        found |> should be True
        i |> should equal 13 // we evaluated to the end
    }


    [<Fact>]
    let ``TaskSeq-forall _specialcase_ prove statement after first false result is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.forall ((>) 0)
        found |> should be False
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' was evaluated

        // find some next item. We do get a new iterator, but mutable state is still starting at '0'
        let! found = ts |> TaskSeq.forall ((>) 4)
        found |> should be False
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-forallAsync _specialcase_ prove statement after first false result is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.forallAsync (fun x -> Task.fromResult (x < 0))
        found |> should be False
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' was evaluated

        // find some next item. We do get a new iterator, but mutable state is still starting at '0'
        let! found = ts |> TaskSeq.forallAsync (fun x -> Task.fromResult (x < 4))
        found |> should be False
        i |> should equal 4 // only partial evaluation!
    }
