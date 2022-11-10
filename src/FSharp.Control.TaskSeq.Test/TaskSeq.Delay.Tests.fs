module TaskSeq.Tests.Delay

open System

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control
open System.Collections.Generic

//
// TaskSeq.delay
//

let validateSequence ts =
    ts
    |> TaskSeq.toListAsync
    |> Task.map (List.map string)
    |> Task.map (String.concat "")
    |> Task.map (should equal "12345678910")

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-delay with empty sequences`` variant =
        fun () -> Gen.getEmptyVariant variant
        |> TaskSeq.delay
        |> verifyEmpty

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-delay`` variant =
        fun () -> Gen.getSeqImmutable variant
        |> TaskSeq.delay
        |> validateSequence

module SideEffect =
    [<Fact>]
    let ``TaskSeq-delay executes side effects`` () = task {
        let mutable i = 0

        let ts =
            fun () -> taskSeq {
                yield! [ 1..10 ]
                i <- i + 1
            }
            |> TaskSeq.delay

        do! ts |> validateSequence
        i |> should equal 1
        let! len = TaskSeq.length ts
        i |> should equal 2 // re-eval of the sequence executes side effect again
        len |> should equal 10
    }
