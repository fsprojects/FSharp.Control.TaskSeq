module FSharpy.Tests.Map

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

//
// TaskSeq.map
// TaskSeq.mapi
// TaskSeq.mapAsync
// TaskSeq.mapiAsync
//

/// Asserts that a sequence contains the char values 'A'..'J'.
let validateSequence ts =
    ts
    |> TaskSeq.toSeqCachedAsync
    |> Task.map (Seq.map string)
    |> Task.map (String.concat "")
    |> Task.map (should equal "ABCDEFGHIJ")

/// Validates for "ABCDEFGHIJ" char sequence, or any amount of char-value higher
let validateSequenceWithOffset offset ts =
    let expected =
        [ 'A' .. 'J' ]
        |> List.map (int >> (+) offset >> char >> string)
        |> String.concat ""

    ts
    |> TaskSeq.toSeqCachedAsync
    |> Task.map (Seq.map string)
    |> Task.map (String.concat "")
    |> Task.map (should equal expected)

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-map maps in correct order`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.map (fun item -> char (item + 64))
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-mapi maps in correct order`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.mapi (fun i _ -> char (i + 65))
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-mapAsync maps in correct order`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.mapAsync (fun item -> task { return char (item + 64) })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-mapiAsync maps in correct order`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.mapiAsync (fun i _ -> task { return char (i + 65) })
        |> verifyEmpty


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-map maps in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.map (fun item -> char (item + 64))
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-mapi maps in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.mapi (fun i _ -> char (i + 65))
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-mapAsync maps in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.mapAsync (fun item -> task { return char (item + 64) })
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-mapiAsync maps in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.mapiAsync (fun i _ -> task { return char (i + 65) })
        |> validateSequence

module SideEffects =
    [<Fact>]
    let ``TaskSeq-map prove that it has no effect until executed`` () =
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1 // we should not get here
            i <- i + 1
            yield 42
            i <- i + 1
        }

        // point of this test: just calling 'map' won't execute anything of the sequence!
        let _ =
            ts
            |> TaskSeq.map (fun x -> x + 10)
            |> TaskSeq.map (fun x -> x + 10)
            |> TaskSeq.map (fun x -> x + 10)

        // multiple maps have no effect unless executed
        i |> should equal 0

    [<Fact>]
    let ``TaskSeq-mapi prove that it has no effect until executed`` () =
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1 // we should not get here
            i <- i + 1
            yield 42
            i <- i + 1
        }

        // point of this test: just calling 'map' won't execute anything of the sequence!
        let _ =
            ts
            |> TaskSeq.mapi (fun x _ -> x + 10)
            |> TaskSeq.mapi (fun x _ -> x + 10)
            |> TaskSeq.mapi (fun x _ -> x + 10)

        // multiple maps have no effect unless executed
        i |> should equal 0

    [<Fact>]
    let ``TaskSeq-mapAsync prove that it has no effect until executed`` () =
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1 // we should not get here
            i <- i + 1
            yield 42
            i <- i + 1
        }

        // point of this test: just calling 'map' won't execute anything of the sequence!
        let _ =
            ts
            |> TaskSeq.mapAsync (fun x -> task { return x + 10 })
            |> TaskSeq.mapAsync (fun x -> task { return x + 10 })
            |> TaskSeq.mapAsync (fun x -> task { return x + 10 })

        // multiple maps have no effect unless executed
        i |> should equal 0

    [<Fact>]
    let ``TaskSeq-mapiAsync prove that it has no effect until executed`` () =
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1 // we should not get here
            i <- i + 1
            yield 42
            i <- i + 1
        }

        // point of this test: just calling 'map' won't execute anything of the sequence!
        let _ =
            ts
            |> TaskSeq.mapiAsync (fun x _ -> task { return x + 10 })
            |> TaskSeq.mapiAsync (fun x _ -> task { return x + 10 })
            |> TaskSeq.mapiAsync (fun x _ -> task { return x + 10 })

        // multiple maps have no effect unless executed
        i |> should equal 0


    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-map can access mutables which are mutated in correct order`` variant =
        let mutable sum = 0

        Gen.getSeqWithSideEffect variant
        |> TaskSeq.map (fun item ->
            sum <- sum + 1
            char (sum + 64))
        |> validateSequence
        |> Task.map (fun () -> sum |> should equal 10)

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-mapi can access mutables which are mutated in correct order`` variant =
        let mutable sum = 0

        Gen.getSeqWithSideEffect variant
        |> TaskSeq.mapi (fun i _ ->
            sum <- i + 1
            char (sum + 64))
        |> validateSequence
        |> Task.map (fun () -> sum |> should equal 10)

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-mapAsync can map the same sequence multiple times`` variant = task {
        let doMap = TaskSeq.mapAsync (fun item -> task { return char (item + 64) })
        let ts = Gen.getSeqWithSideEffect variant

        // each time we do GetAsyncEnumerator(), and go through the whole sequence,
        // the whole sequence gets re-evaluated, causing our +1 side-effect to run again.
        do! doMap ts |> validateSequence
        do! doMap ts |> validateSequenceWithOffset 10 // the mutable is 10 higher
        do! doMap ts |> validateSequenceWithOffset 20 // again
        do! doMap ts |> validateSequenceWithOffset 30 // again
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-mapAsync can access mutables which are mutated in correct order`` variant =
        let mutable sum = 0

        Gen.getSeqWithSideEffect variant
        |> TaskSeq.mapAsync (fun item -> task {
            sum <- sum + 1
            return char (sum + 64)
        })
        |> validateSequence
        |> Task.map (fun () -> sum |> should equal 10)

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-mapiAsync can access mutables which are mutated in correct order`` variant =
        let mutable data = '0'

        Gen.getSeqWithSideEffect variant
        |> TaskSeq.mapiAsync (fun i _ -> task {
            data <- char (i + 65)
            return data
        })
        |> validateSequence
        |> Task.map (fun () -> data |> should equal (char 74))
