module TaskSeq.Tests.Append

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.append
// TaskSeq.appendSeq
// TaskSeq.prependSeq
//

let validateSequence ts =
    ts
    |> TaskSeq.toListAsync
    |> Task.map (List.map string)
    |> Task.map (String.concat "")
    |> Task.map (should equal "1234567891012345678910")


module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.empty |> TaskSeq.append null

        assertNullArg
        <| fun () -> null |> TaskSeq.append TaskSeq.empty

        assertNullArg <| fun () -> null |> TaskSeq.append null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-append both args empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.append (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-appendSeq both args empty`` variant =
        Seq.empty
        |> TaskSeq.appendSeq (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-prependSeq both args empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.prependSeq Seq.empty
        |> verifyEmpty

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-append`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.append (Gen.getSeqImmutable variant)
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-appendSeq with a list`` variant =
        [ 1..10 ]
        |> TaskSeq.appendSeq (Gen.getSeqImmutable variant)
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-appendSeq with an array`` variant =
        [| 1..10 |]
        |> TaskSeq.appendSeq (Gen.getSeqImmutable variant)
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-prependSeq with a list`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.prependSeq [ 1..10 ]
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-prependSeq with an array`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.prependSeq [| 1..10 |]
        |> validateSequence

module SideEffects =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-append consumes whole sequence once incl after-effects`` variant =
        let mutable i = 0

        taskSeq {
            i <- i + 1
            yield! [ 1..10 ]
            i <- i + 1
        }
        |> TaskSeq.append (Gen.getSeqImmutable variant)
        |> validateSequence
        |> Task.map (fun () -> i |> should equal 2)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-appendSeq consumes whole sequence once incl after-effects`` variant =
        // TODO use variant
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            yield! [ 1..10 ]
            i <- i + 1
        }

        [| 1..10 |]
        |> TaskSeq.appendSeq ts
        |> validateSequence
        |> Task.map (fun () -> i |> should equal 2)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-prependSeq consumes whole sequence once incl after-effects`` variant =
        // TODO use variant
        let mutable i = 0

        taskSeq {
            i <- i + 1
            yield! [ 1..10 ]
            i <- i + 1
        }
        |> TaskSeq.prependSeq [ 1..10 ]
        |> validateSequence
        |> Task.map (fun () -> i |> should equal 2)
