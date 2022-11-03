module FSharpy.Tests.IsEmpty

open System.Threading.Tasks
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

module EmptySeq =

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-isEmpty returns true for empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.isEmpty
        |> Task.map (should be True)

module Immutable =
    [<Fact>]
    let ``TaskSeq-isEmpty returns false for singleton`` () =
        taskSeq { yield 42 }
        |> TaskSeq.isEmpty
        |> Task.map (should be False)

    [<Fact>]
    let ``TaskSeq-isEmpty returns false for delayed singleton sequence`` () =
        Gen.sideEffectTaskSeqMs 200<ms> 400<ms> 3
        |> TaskSeq.isEmpty
        |> Task.map (should be False)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-isEmpty returns false for non-empty`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.isEmpty
        |> Task.map (should be False)

module SideEffects =
    [<Fact>]
    let ``TaskSeq-isEmpty prove that it won't execute side effects after the first item`` () =
        let mutable i = 0

        taskSeq {
            i <- i + 1
            yield 42
            i <- i + 1
        }
        |> TaskSeq.isEmpty
        |> Task.map (should be False)
        |> Task.map (fun () -> i |> should equal 1)

    [<Fact>]
    let ``TaskSeq-isEmpty prove that it does execute side effects if empty`` () =
        let mutable i = 0

        taskSeq {
            i <- i + 1
            i <- i + 1
        }
        |> TaskSeq.isEmpty
        |> Task.map (should be True)
        |> Task.map (fun () -> i |> should equal 2)

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-isEmpty returns false for non-empty`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.isEmpty
        |> Task.map (should be False)
