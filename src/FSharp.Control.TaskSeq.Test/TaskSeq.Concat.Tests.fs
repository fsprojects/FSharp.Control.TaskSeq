module TaskSeq.Tests.Concat

open System

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control
open System.Collections.Generic

//
// TaskSeq.concat
//

let validateSequence ts =
    ts
    |> TaskSeq.toListAsync
    |> Task.map (List.map string)
    |> Task.map (String.concat "")
    |> Task.map (should equal "123456789101234567891012345678910")

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () = assertNullArg <| fun () -> TaskSeq.concat null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-concat with empty sequences`` variant =
        taskSeq {
            yield Gen.getEmptyVariant variant // not yield-bang!
            yield Gen.getEmptyVariant variant
            yield Gen.getEmptyVariant variant
        }
        |> TaskSeq.concat
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-concat with top sequence empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.cast<IAsyncEnumerable<int>> // casting an int to an enumerable, LOL!
        |> TaskSeq.concat
        |> verifyEmpty

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-concat with three sequences of sequences`` variant =
        taskSeq {
            yield Gen.getSeqImmutable variant // not yield-bang!
            yield Gen.getSeqImmutable variant
            yield Gen.getSeqImmutable variant
        }
        |> TaskSeq.concat
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-concat with three sequences of sequences and few empties`` variant =
        taskSeq {
            yield TaskSeq.empty
            yield Gen.getSeqImmutable variant // not yield-bang!
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield Gen.getSeqImmutable variant
            yield TaskSeq.empty
            yield Gen.getSeqImmutable variant
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield TaskSeq.empty
        }
        |> TaskSeq.concat
        |> validateSequence

module SideEffect =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-concat consumes until the end, including side-effects`` variant =
        let mutable i = 0

        taskSeq {
            yield Gen.getSeqImmutable variant // not yield-bang!
            yield Gen.getSeqImmutable variant

            yield taskSeq {
                yield! [ 1..10 ]
                i <- i + 1
            }
        }
        |> TaskSeq.concat
        |> validateSequence
        |> Task.map (fun () -> i |> should equal 1)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-concat consumes side effects in empty sequences`` variant =
        let mutable i = 0

        taskSeq {
            yield taskSeq { do i <- i + 1 }
            yield Gen.getSeqImmutable variant // not yield-bang!
            yield TaskSeq.empty
            yield taskSeq { do i <- i + 1 }
            yield Gen.getSeqImmutable variant
            yield TaskSeq.empty
            yield Gen.getSeqImmutable variant
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield taskSeq { do i <- i + 1 }
        }
        |> TaskSeq.concat
        |> validateSequence
        |> Task.map (fun () -> i |> should equal 3)
