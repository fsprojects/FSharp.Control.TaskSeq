module TaskSeq.Tests.Collect

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.collect
// TaskSeq.collectAsync
//

module EmptySeq =

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collect collecting emptiness`` variant =
        Gen.sideEffectTaskSeq 10
        |> TaskSeq.collect (fun _ -> Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-collect collecting emptiness v2`` variant =
        Gen.sideEffectTaskSeq variant
        |> TaskSeq.collect (fun _ -> Gen.getEmptyVariant EmptyVariant.YieldBang)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collect collecting emptiness from emptiness`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.collect (fun _ -> Gen.getEmptyVariant variant)
        |> verifyEmpty


    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collect collecting non-empty sequences on an empty sequence`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.collect (fun _ -> taskSeq { yield 10 })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collectAsync collecting emptiness`` variant =
        Gen.sideEffectTaskSeq 10
        |> TaskSeq.collectAsync (fun _ -> task { return Gen.getEmptyVariant variant })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-collectAsync collecting emptiness v2`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.collectAsync (fun _ -> task { return Gen.getEmptyVariant EmptyVariant.DelayDoBang })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collectAsync collecting emptiness from emptiness`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.collectAsync (fun _ -> task { return Gen.getEmptyVariant variant })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collectAsync collecting non-empty sequences on an empty sequence`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.collectAsync (fun _ -> task { return taskSeq { yield 10 } })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-collectSeq collecting emptiness`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.collectSeq (fun _ -> Seq.empty<int>)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collectSeq collecting emptiness from emptiness`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.collectSeq (fun _ -> Seq.empty<int>)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collectSeq collecting non-empty sequences on an empty sequence`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.collectSeq (fun _ -> seq { yield 10 })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-collectSeqAsync collecting emptiness`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.collectSeqAsync (fun _ -> task { return Array.empty<int> })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collectSeqAsync collecting emptiness from emptiness`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.collectSeqAsync (fun _ -> task { return Array.empty<int> })
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-collectSeqAsync collecting non-empty sequences on an empty sequence`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.collectSeqAsync (fun _ -> task { return [ 10 ] })
        |> verifyEmpty

module Immutable =

    let validateSequence ts =
        ts
        |> TaskSeq.toListAsync
        |> Task.map (List.map string)
        |> Task.map (String.concat "")
        |> Task.map (should equal "ABBCCDDEEFFGGHHIIJJK")

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-collect operates in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.collect (fun item -> taskSeq {
            yield char (item + 64)
            yield char (item + 65)
        })
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-collectAsync operates in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.collectAsync (fun item -> task {
            return taskSeq {
                yield char (item + 64)
                yield char (item + 65)
            }
        })
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-collectSeq operates in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.collectSeq (fun item -> seq {
            yield char (item + 64)
            yield char (item + 65)
        })
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-collectSeq with arrays operates in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.collectSeq (fun item -> [| char (item + 64); char (item + 65) |])
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-collectSeqAsync operates in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.collectSeqAsync (fun item -> task {
            return seq {
                yield char (item + 64)
                yield char (item + 65)
            }
        })
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-collectSeqAsync with arrays operates in correct order`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.collectSeqAsync (fun item -> task { return [| char (item + 64); char (item + 65) |] })
        |> validateSequence

module SideEffects =
    [<Fact>]
    let ``TaskSeq-collect prove that it has no effect until executed`` () =
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
            |> TaskSeq.collect (fun x -> taskSeq { yield 10 })
            |> TaskSeq.collect (fun x -> taskSeq { yield 10 })
            |> TaskSeq.collect (fun x -> taskSeq { yield 10 })

        // multiple maps have no effect unless executed
        i |> should equal 0

    [<Fact>]
    let ``TaskSeq-collectAsync prove that it has no effect until executed`` () =
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
            |> TaskSeq.collectAsync (fun x -> task { return taskSeq { yield 10 } })
            |> TaskSeq.collectAsync (fun x -> task { return taskSeq { yield 10 } })
            |> TaskSeq.collectAsync (fun x -> task { return taskSeq { yield 10 } })

        // multiple maps have no effect unless executed
        i |> should equal 0

    [<Fact>]
    let ``TaskSeq-collectSeq prove that it has no effect until executed`` () =
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
            |> TaskSeq.collectSeq (fun x -> seq { yield 10 })
            |> TaskSeq.collectSeq (fun x -> seq { yield 10 })
            |> TaskSeq.collectSeq (fun x -> seq { yield 10 })

        // multiple maps have no effect unless executed
        i |> should equal 0

    [<Fact>]
    let ``TaskSeq-collectSeqAsync prove that it has no effect until executed`` () =
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
            |> TaskSeq.collectSeqAsync (fun x -> task { return seq { yield 10 } })
            |> TaskSeq.collectSeqAsync (fun x -> task { return seq { yield 10 } })
            |> TaskSeq.collectSeqAsync (fun x -> task { return seq { yield 10 } })

        // multiple maps have no effect unless executed
        i |> should equal 0
