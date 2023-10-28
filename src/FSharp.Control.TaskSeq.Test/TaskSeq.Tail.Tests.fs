module TaskSeq.Tests.Tail

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.tail
// TaskSeq.tryTail
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg <| fun () -> TaskSeq.tail null
        assertNullArg <| fun () -> TaskSeq.tryTail null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tail throws`` variant = task {
        fun () -> Gen.getEmptyVariant variant |> TaskSeq.tail |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryTail returns None`` variant = task {
        let! nothing = Gen.getEmptyVariant variant |> TaskSeq.tryTail
        nothing |> should be None'
    }

    [<Fact>]
    let ``TaskSeq-tail executes side effect`` () = task {
        let mutable x = 0

        fun () -> taskSeq { do x <- x + 1 } |> TaskSeq.tail |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        // side effect must have run!
        x |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-tryTail executes side effect`` () = task {
        let mutable x = 0

        let! nothing = taskSeq { do x <- x + 1 } |> TaskSeq.tryTail
        nothing |> should be None'

        // side effect must have run!
        x |> should equal 1
    }


module Immutable =
    let verifyTail tail =
        tail
        |> TaskSeq.toArrayAsync
        |> Task.map (should equal [| 2..10 |])

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tail gets the tail items`` variant = task {
        let ts = Gen.getSeqImmutable variant

        let! tail = TaskSeq.tail ts
        do! verifyTail tail

        let! tail = TaskSeq.tail ts //immutable, so re-iteration does not change outcome
        do! verifyTail tail
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryTail gets the tail item`` variant = task {
        let ts = Gen.getSeqImmutable variant

        match! TaskSeq.tryTail ts with
        | Some tail -> do! verifyTail tail
        | x -> do x |> should not' (be None')

    }

    [<Fact>]
    let ``TaskSeq-tail return empty from a singleton sequence`` () = task {
        let ts = taskSeq { yield 42 }

        let! tail = TaskSeq.tail ts
        do! verifyEmpty tail
    }

    [<Fact>]
    let ``TaskSeq-tryTail gets the only item in a singleton sequence`` () = task {
        let ts = taskSeq { yield 42 }

        match! TaskSeq.tryTail ts with
        | Some tail -> do! verifyEmpty tail
        | x -> do x |> should not' (be None')
    }


module SideEffects =
    [<Fact>]
    let ``TaskSeq-tail does not execute side effect after the first item in singleton`` () = task {
        let mutable x = 42

        let one = taskSeq {
            yield x
            x <- x + 1 // <--- we should never get here
        }

        let! _ = one |> TaskSeq.tail
        let! _ = one |> TaskSeq.tail // side effect, re-iterating!

        x |> should equal 42
    }

    [<Fact>]
    let ``TaskSeq-tryTail does not execute execute side effect after first item in singleton`` () = task {
        let mutable x = 42

        let one = taskSeq {
            yield x
            x <- x + 1 // <--- we should never get here
        }

        let! _ = one |> TaskSeq.tryTail
        let! _ = one |> TaskSeq.tryTail

        // side effect, reiterating causes it to execute again!
        x |> should equal 42

    }

    [<Fact>]
    let ``TaskSeq-tail executes side effect partially`` () = task {
        let mutable x = 42

        let ts = taskSeq {
            x <- x + 1 // <--- executed on tail, but not materializing rest
            yield 1
            x <- x + 1 // <--- not executed on tail, but on materializing rest
            yield 2
            x <- x + 1 // <--- id
        }

        let! tail1 = ts |> TaskSeq.tail
        x |> should equal 43 // test side effect runs 1x

        let! tail2 = ts |> TaskSeq.tail
        x |> should equal 44 // test side effect ran again only 1x

        let! len = TaskSeq.length tail1
        x |> should equal 46 // now 2nd & 3rd side effect runs, but not the first
        len |> should equal 1

        let! len = TaskSeq.length tail2
        x |> should equal 48 // now again 2nd & 3rd side effect runs, but not the first
        len |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-tryTail executes side effect partially`` () = task {
        let mutable x = 42

        let ts = taskSeq {
            x <- x + 1 // <--- executed on tail, but not materializing rest
            yield 1
            x <- x + 1 // <--- not executed on tail, but on materializing rest
            yield 2
            x <- x + 1 // <--- id
        }

        let! tail1 = ts |> TaskSeq.tryTail
        x |> should equal 43 // test side effect runs 1x

        let! tail2 = ts |> TaskSeq.tryTail
        x |> should equal 44 // test side effect ran again only 1x

        let! len = TaskSeq.length tail1.Value
        x |> should equal 46 // now 2nd side effect runs, but not the first
        len |> should equal 1

        let! len = TaskSeq.length tail2.Value
        x |> should equal 48 // now again 2nd side effect runs, but not the first
        len |> should equal 1
    }
