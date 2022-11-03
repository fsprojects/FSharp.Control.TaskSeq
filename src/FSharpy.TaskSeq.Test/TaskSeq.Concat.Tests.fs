module FSharpy.Tests.Concat

open System

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy
open System.Collections.Generic

//
// TaskSeq.concat
//

let validateSequence ts =
    ts
    |> TaskSeq.toSeqCachedAsync
    |> Task.map (Seq.map string)
    |> Task.map (String.concat "")
    |> Task.map (should equal "123456789101234567891012345678910")

module EmptySeq =
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
    let ``TaskSeq-concat with empty sequences`` variant =
        taskSeq {
            yield Gen.getSeqImmutable variant // not yield-bang!
            yield Gen.getSeqImmutable variant
            yield Gen.getSeqImmutable variant
        }
        |> TaskSeq.concat
        |> validateSequence
