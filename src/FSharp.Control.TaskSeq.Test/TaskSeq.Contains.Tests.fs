module TaskSeq.Tests.Contains

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.contains
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () = assertNullArg <| fun () -> TaskSeq.contains 42 null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-contains returns false`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.contains 12
        |> Task.map (should be False)

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-contains sad path returns false`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.contains 0
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-contains happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.contains 5
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-contains happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.contains 1
        |> Task.map (should be True)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-contains happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.contains 10
        |> Task.map (should be True)

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-contains KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        // first: false
        let! found = TaskSeq.contains 11 ts
        found |> should be False

        // find again: found now, because of side effects
        let! found = TaskSeq.contains 11 ts
        found |> should be True

        // find once more: false
        let! found = TaskSeq.contains 11 ts
        found |> should be False
    }

    [<Fact>]
    let ``TaskSeq-contains _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.contains 3
        found |> should be True
        i |> should equal 3 // only partial evaluation!

        // find next item. We do get a new iterator, but mutable state is now starting at '3', so first item now returned is '4'.
        let! found = ts |> TaskSeq.contains 4
        found |> should be True
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-contains _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.contains 42
        found |> should be True
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-contains _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.contains 0
        found |> should be True
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // find some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.contains 4
        found |> should be True
        i |> should equal 4 // only partial evaluation!
    }
