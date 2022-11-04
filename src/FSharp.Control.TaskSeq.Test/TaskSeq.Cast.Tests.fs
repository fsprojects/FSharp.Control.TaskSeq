module TaskSeq.Tests.Cast

open System

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.box
// TaskSeq.unbox
// TaskSeq.cast
//

/// Asserts that a sequence contains the char values 'A'..'J'.
let validateSequence ts =
    ts
    |> TaskSeq.toSeqCachedAsync
    |> Task.map (Seq.map string)
    |> Task.map (String.concat "")
    |> Task.map (should equal "12345678910")

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-box empty`` variant = Gen.getEmptyVariant variant |> TaskSeq.box |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-unbox empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.unbox<int>
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-cast empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.cast<int>
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-unbox empty to invalid type should not fail`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.unbox<Guid> // cannot cast to int, but for empty sequences, the exception won't be thrown
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-cast empty to invalid type should not fail`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.cast<string> // cannot cast to int, but for empty sequences, the exception won't be thrown
        |> verifyEmpty

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-box`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.box
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-unbox`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.box
        |> TaskSeq.unbox<int>
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-cast`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.box
        |> TaskSeq.cast<int>
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-unbox invalid type should throw`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.box
            |> TaskSeq.unbox<uint> // cannot unbox from int to uint, even though types have the same size
            |> TaskSeq.toArrayAsync
            |> Task.ignore

        |> should throwAsyncExact typeof<InvalidCastException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-cast invalid type should throw`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.box
            |> TaskSeq.cast<string>
            |> TaskSeq.toArrayAsync
            |> Task.ignore

        |> should throwAsyncExact typeof<InvalidCastException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-unbox invalid type should NOT throw before sequence is iterated`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.box
            |> TaskSeq.unbox<uint> // no iteration done
            |> ignore

        |> should not' (throw typeof<Exception>)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-cast invalid type should NOT throw before sequence is iterated`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.box
            |> TaskSeq.cast<string> // no iteration done
            |> ignore

        |> should not' (throw typeof<Exception>)

module SideEffects =
    [<Fact>]
    let ``TaskSeq-box prove that it has no effect until executed`` () =
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1 // we should not get here
            i <- i + 1
            yield 42
            i <- i + 1
        }

        // point of this test: just calling 'box' won't execute anything of the sequence!
        let boxed = ts |> TaskSeq.box |> TaskSeq.box |> TaskSeq.box

        // no side effect until iterated
        i |> should equal 0

        boxed
        |> TaskSeq.last
        |> Task.map (should equal 42)
        |> Task.map (fun () -> i = 9)

    [<Fact>]
    let ``TaskSeq-unbox prove that it has no effect until executed`` () =
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1 // we should not get here
            i <- i + 1
            yield box 42
            i <- i + 1
        }

        // point of this test: just calling 'unbox' won't execute anything of the sequence!
        let unboxed = ts |> TaskSeq.unbox

        // no side effect until iterated
        i |> should equal 0

        unboxed
        |> TaskSeq.last
        |> Task.map (should equal 42)
        |> Task.map (fun () -> i = 3)

    [<Fact>]
    let ``TaskSeq-cast prove that it has no effect until executed`` () =
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1 // we should not get here
            i <- i + 1
            yield box 42
            i <- i + 1
        }

        // point of this test: just calling 'cast' won't execute anything of the sequence!
        let cast = ts |> TaskSeq.cast<int>
        i |> should equal 0 // no side effect until iterated

        cast
        |> TaskSeq.last
        |> Task.map (should equal 42)
        |> Task.map (fun () -> i = 3)
