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
