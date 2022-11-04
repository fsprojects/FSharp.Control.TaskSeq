module TaskSeq.Tests.Head

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.head
// TaskSeq.tryHead
//

module EmptySeq =

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-head throws`` variant = task {
        fun () -> Gen.getEmptyVariant variant |> TaskSeq.head |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryHead returns None`` variant = task {
        let! nothing = Gen.getEmptyVariant variant |> TaskSeq.tryHead
        nothing |> should be None'
    }

    [<Fact>]
    let ``TaskSeq-head throws, but side effect is executed`` () = task {
        let mutable x = 0

        fun () -> taskSeq { do x <- x + 1 } |> TaskSeq.head |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        // side effect must have run!
        x |> should equal 1
    }


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-head gets the head item of longer sequence`` variant = task {
        let ts = Gen.getSeqImmutable variant

        let! head = TaskSeq.head ts
        head |> should equal 1

        let! head = TaskSeq.head ts //immutable, so re-iteration does not change outcome
        head |> should equal 1
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryHead gets the head item of longer sequence`` variant = task {
        let ts = Gen.getSeqImmutable variant

        let! head = TaskSeq.tryHead ts
        head |> should equal (Some 1)

        let! head = TaskSeq.tryHead ts //immutable, so re-iteration does not change outcome
        head |> should equal (Some 1)
    }

    [<Fact>]
    let ``TaskSeq-head gets the only item in a singleton sequence`` () = task {
        let ts = taskSeq { yield 42 }

        let! head = TaskSeq.head ts
        head |> should equal 42

        let! head = TaskSeq.head ts // doing it twice is fine
        head |> should equal 42
    }

    [<Fact>]
    let ``TaskSeq-tryHead gets the only item in a singleton sequence`` () = task {
        let ts = taskSeq { yield 42 }

        let! head = TaskSeq.tryHead ts
        head |> should equal (Some 42)

        let! head = TaskSeq.tryHead ts // doing it twice is fine
        head |> should equal (Some 42)
    }


module SideEffects =
    [<Fact>]
    let ``TaskSeq-head __special-case__ prove it does not read beyond first yield`` () = task {
        let mutable x = 42

        let one = taskSeq {
            yield x
            x <- x + 1 // we never get here
        }

        let! fortytwo = one |> TaskSeq.head
        let! stillFortyTwo = one |> TaskSeq.head // the statement after 'yield' will never be reached

        fortytwo |> should equal 42
        stillFortyTwo |> should equal 42
    }

    [<Fact>]
    let ``TaskSeq-tryHead __special-case__ prove it does not read beyond first yield`` () = task {
        let mutable x = 42

        let one = taskSeq {
            yield x
            x <- x + 1 // we never get here
        }

        let! fortytwo = one |> TaskSeq.tryHead
        fortytwo |> should equal (Some 42)

        // the statement after 'yield' will never be reached, the mutable will not be updated
        let! stillFortyTwo = one |> TaskSeq.tryHead
        stillFortyTwo |> should equal (Some 42)

    }

    [<Fact>]
    let ``TaskSeq-head __special-case__ prove early side effect is executed`` () = task {
        let mutable x = 42

        let one = taskSeq {
            x <- x + 1
            x <- x + 1
            yield 42
            x <- x + 200 // we won't get here!
        }

        let! fortyTwo = one |> TaskSeq.head
        fortyTwo |> should equal 42
        x |> should equal 44
        let! fortyTwo = one |> TaskSeq.head
        fortyTwo |> should equal 42
        x |> should equal 46
    }

    [<Fact>]
    let ``TaskSeq-tryHead __special-case__ prove early side effect is executed`` () = task {
        let mutable x = 42

        let one = taskSeq {
            x <- x + 1
            x <- x + 1
            yield 42
            x <- x + 200 // we won't get here!
        }

        let! fortyTwo = one |> TaskSeq.tryHead
        fortyTwo |> should equal (Some 42)
        x |> should equal 44
        let! fortyTwo = one |> TaskSeq.tryHead
        fortyTwo |> should equal (Some 42)
        x |> should equal 46

    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-head gets the head item in a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! ten = TaskSeq.head ts
        ten |> should equal 1

        // side effect, reiterating causes it to execute again!
        let! twenty = TaskSeq.head ts
        twenty |> should not' (equal 1) // different test data changes first item counter differently
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryHead gets the head item in a longer sequence, with mutation`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! ten = TaskSeq.tryHead ts
        ten |> should equal (Some 1)

        // side effect, reiterating causes it to execute again!
        let! twenty = TaskSeq.tryHead ts
        twenty |> should not' (equal (Some 1)) // different test data changes first item counter differently
    }
