module TaskSeq.Tests.FirstLastDefault

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.firstOrDefault
// TaskSeq.lastOrDefault
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg <| fun () -> TaskSeq.firstOrDefault 0 null
        assertNullArg <| fun () -> TaskSeq.lastOrDefault 0 null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-firstOrDefault returns default for empty`` variant = task {
        let! result = Gen.getEmptyVariant variant |> TaskSeq.firstOrDefault 42
        result |> should equal 42
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-lastOrDefault returns default for empty`` variant = task {
        let! result = Gen.getEmptyVariant variant |> TaskSeq.lastOrDefault 99
        result |> should equal 99
    }

    [<Fact>]
    let ``TaskSeq-firstOrDefault returns default with reference type`` () = task {
        let! result = TaskSeq.empty<string> |> TaskSeq.firstOrDefault "hello"
        result |> should equal "hello"
    }

    [<Fact>]
    let ``TaskSeq-lastOrDefault returns default with reference type`` () = task {
        let! result = TaskSeq.empty<string> |> TaskSeq.lastOrDefault "world"
        result |> should equal "world"
    }


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-firstOrDefault returns first element`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! result = TaskSeq.firstOrDefault 0 ts
        result |> should equal 1
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lastOrDefault returns last element`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! result = TaskSeq.lastOrDefault 0 ts
        result |> should equal 10
    }

    [<Fact>]
    let ``TaskSeq-firstOrDefault does not use default when non-empty`` () = task {
        let! result =
            taskSeq {
                yield 5
                yield 6
            }
            |> TaskSeq.firstOrDefault -1

        result |> should equal 5
    }

    [<Fact>]
    let ``TaskSeq-lastOrDefault does not use default when non-empty`` () = task {
        let! result =
            taskSeq {
                yield 5
                yield 6
            }
            |> TaskSeq.lastOrDefault -1

        result |> should equal 6
    }

    [<Fact>]
    let ``TaskSeq-firstOrDefault with singleton`` () = task {
        let! result = TaskSeq.singleton 42 |> TaskSeq.firstOrDefault 0
        result |> should equal 42
    }

    [<Fact>]
    let ``TaskSeq-lastOrDefault with singleton`` () = task {
        let! result = TaskSeq.singleton 42 |> TaskSeq.lastOrDefault 0
        result |> should equal 42
    }


module SideEffects =
    [<Fact>]
    let ``TaskSeq-firstOrDefault __special-case__ prove it does not read beyond first yield`` () = task {
        let mutable x = 42

        let ts = taskSeq {
            yield x
            x <- x + 1 // we never get here
        }

        let! fortyTwo = ts |> TaskSeq.firstOrDefault 0
        let! stillFortyTwo = ts |> TaskSeq.firstOrDefault 0 // the statement after 'yield' will never be reached

        fortyTwo |> should equal 42
        stillFortyTwo |> should equal 42
    }

    [<Fact>]
    let ``TaskSeq-firstOrDefault __special-case__ prove early side effect is executed`` () = task {
        let mutable x = 42

        let ts = taskSeq {
            x <- x + 1
            x <- x + 1
            yield 42
            x <- x + 200 // we won't get here!
        }

        let! result = ts |> TaskSeq.firstOrDefault 0
        result |> should equal 42
        x |> should equal 44

        let! result = ts |> TaskSeq.firstOrDefault 0
        result |> should equal 42
        x |> should equal 46
    }

    [<Fact>]
    let ``TaskSeq-lastOrDefault __special-case__ prove it reads the entire sequence`` () = task {
        let mutable x = 42

        let ts = taskSeq {
            yield x
            x <- x + 1 // will be executed
            yield x
            x <- x + 1 // will be executed
        }

        let! result = ts |> TaskSeq.lastOrDefault -1
        result |> should equal 43
        x |> should equal 44
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-firstOrDefault returns first item in a side-effect sequence`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! first = ts |> TaskSeq.firstOrDefault 0
        first |> should equal 1

        // side effect: re-enumerating changes the first item
        let! secondFirst = ts |> TaskSeq.firstOrDefault 0
        secondFirst |> should not' (equal 1)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lastOrDefault returns last item and exhausts the sequence`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! last = ts |> TaskSeq.lastOrDefault 0
        last |> should equal 10

        // side effect: re-enumerating continues from mutated state
        let! secondLast = ts |> TaskSeq.lastOrDefault 0
        secondLast |> should equal 20
    }
