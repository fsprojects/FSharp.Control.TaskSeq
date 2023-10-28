module TaskSeq.Tests.Indexed

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.indexed
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () = assertNullArg <| fun () -> TaskSeq.indexed null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-indexed on empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.indexed
        |> verifyEmpty

module Immutable =
    [<Fact>]
    let ``TaskSeq-indexed starts at zero`` () =
        taskSeq { yield 99 }
        |> TaskSeq.indexed
        |> TaskSeq.head
        |> Task.map (should equal (0, 99))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-indexed`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.indexed
        |> TaskSeq.toArrayAsync
        |> Task.map (Array.forall (fun (x, y) -> x + 1 = y))
        |> Task.map (should be True)
