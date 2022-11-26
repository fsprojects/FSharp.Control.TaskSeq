module TaskSeq.Tests.TaskExtensions

open System
open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// Task extensions
// Async extensions
//


module TaskCE =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``Task-for CE with taskSeq`` variant = task {
        let values = Gen.getSeqImmutable variant

        let mutable sum = 0

        for x in values do
            sum <- sum + x

        sum |> should equal 55
    }

module AsyncCE =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``Async-for CE with taskSeq`` variant = async {
        let values = Gen.getSeqImmutable variant

        let mutable sum = 0

        for x in values do
            sum <- sum + x

        sum |> should equal 55
    }
