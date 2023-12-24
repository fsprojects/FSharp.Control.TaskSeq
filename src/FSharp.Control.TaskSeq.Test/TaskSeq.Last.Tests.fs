module TaskSeq.Tests.Last

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.last
// TaskSeq.tryLast
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg <| fun () -> TaskSeq.last null
        assertNullArg <| fun () -> TaskSeq.tryLast null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-last throws`` variant = task {
        fun () -> Gen.getEmptyVariant variant |> TaskSeq.last |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryLast returns None`` variant = task {
        let! nothing = Gen.getEmptyVariant variant |> TaskSeq.tryLast
        nothing |> should be None'
    }

    [<Fact>]
    let ``TaskSeq-last executes side effect`` () = task {
        let mutable x = 0

        fun () -> taskSeq { do x <- x + 1 } |> TaskSeq.last |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        // side effect must have run!
        x |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-tryLast executes side effect`` () = task {
        let mutable x = 0

        let! nothing = taskSeq { do x <- x + 1 } |> TaskSeq.tryLast
        nothing |> should be None'

        // side effect must have run!
        x |> should equal 1
    }


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-last gets the last item`` variant = task {
        let ts = Gen.getSeqImmutable variant

        let! last = TaskSeq.last ts
        last |> should equal 10

        let! last = TaskSeq.last ts //immutable, so re-iteration does not change outcome
        last |> should equal 10
    }

    [<Fact>]
    let ``TaskSeq-last gets the only item in a singleton sequence`` () = task {
        let ts = taskSeq { yield 42 }

        let! last = TaskSeq.last ts
        last |> should equal 42

        let! last = TaskSeq.last ts // doing it twice is fine
        last |> should equal 42
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryLast gets the last item`` variant = task {
        let ts = Gen.getSeqImmutable variant

        let! last = TaskSeq.tryLast ts
        last |> should equal (Some 10)

        let! last = TaskSeq.tryLast ts //immutable, so re-iteration does not change outcome
        last |> should equal (Some 10)
    }

    [<Fact>]
    let ``TaskSeq-tryLast gets the only item in a singleton sequence`` () = task {
        let ts = taskSeq { yield 42 }

        let! last = TaskSeq.tryLast ts
        last |> should equal (Some 42)

        let! last = TaskSeq.tryLast ts // doing it twice is fine
        last |> should equal (Some 42)
    }


module SideEffects =
    [<Fact>]
    let ``TaskSeq-last executes side effect after first item`` () = task {
        let mutable x = 42

        let one = taskSeq {
            yield x
            x <- x + 1
        }

        let! fortyTwo = one |> TaskSeq.last
        let! fortyThree = one |> TaskSeq.last // side effect, re-iterating!

        fortyTwo |> should equal 42
        fortyThree |> should equal 43
    }

    [<Fact>]
    let ``TaskSeq-tryLast executes side effect after first item`` () = task {
        let mutable x = 42

        let one = taskSeq {
            yield x
            x <- x + 1
        }

        let! fortyTwo = one |> TaskSeq.tryLast
        fortyTwo |> should equal (Some 42)

        // side effect, reiterating causes it to execute again!
        let! fortyThree = one |> TaskSeq.tryLast
        fortyThree |> should equal (Some 43)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-last gets the last item`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! ten = TaskSeq.last ts
        ten |> should equal 10

        // side effect, reiterating causes it to execute again!
        let! twenty = TaskSeq.last ts
        twenty |> should equal 20
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryLast gets the last item`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! ten = TaskSeq.tryLast ts
        ten |> should equal (Some 10)

        // side effect, reiterating causes it to execute again!
        let! twenty = TaskSeq.tryLast ts
        twenty |> should equal (Some 20)
    }
