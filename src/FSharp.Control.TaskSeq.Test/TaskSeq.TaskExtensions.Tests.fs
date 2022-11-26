module TaskSeq.Tests.TaskExtensions

open System
open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// Task extensions
//

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``Task-for CE with empty taskSeq`` variant = task {
        let values = Gen.getEmptyVariant variant

        let mutable sum = 42

        for x in values do
            sum <- sum + x

        sum |> should equal 42
    }

    [<Fact>]
    let ``Task-for CE must execute side effect in empty taskseq`` () = task {
        let mutable data = 0
        let values = taskSeq { do data <- 42 }

        for x in values do
            ()

        data |> should equal 42
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``Task-for CE with taskSeq`` variant = task {
        let values = Gen.getSeqImmutable variant

        let mutable sum = 0

        for x in values do
            sum <- sum + x

        sum |> should equal 55
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``Task-for CE with taskSeq multiple iterations`` variant = task {
        let values = Gen.getSeqImmutable variant

        let mutable sum = 0

        for x in values do
            sum <- sum + x

        // each following iteration should start at the beginning
        for x in values do
            sum <- sum + x

        for x in values do
            sum <- sum + x

        sum |> should equal 165
    }

    [<Fact>]
    let ``Task-for mixing both types of for loops`` () = async {
        // this test ensures overload resolution is correct
        let ts = TaskSeq.singleton 20
        let sq = Seq.singleton 20
        let mutable sum = 2

        for x in ts do
            sum <- sum + x

        for x in sq do
            sum <- sum + x

        sum |> should equal 42
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``Task-for CE with taskSeq`` variant = task {
        let values = Gen.getSeqWithSideEffect variant

        let mutable sum = 0

        for x in values do
            sum <- sum + x

        sum |> should equal 55
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``Task-for CE with taskSeq multiple iterations`` variant = task {
        let values = Gen.getSeqWithSideEffect variant

        let mutable sum = 0

        for x in values do
            sum <- sum + x

        // each following iteration should start at the beginning
        // with the "side effect" tests, the mutable state updates
        for x in values do
            sum <- sum + x // starts at 11

        for x in values do
            sum <- sum + x // starts at 21

        sum |> should equal 465 // eq to: List.sum [1..30]
    }

module Other =
    [<Fact>]
    let ``Task-for CE must call dispose in empty taskSeq`` () = async {
        let disposed = ref 0
        let values = Gen.getEmptyDisposableTaskSeq disposed

        for x in values do
            ()

        // the DisposeAsync should be called by now
        disposed.Value |> should equal 1
    }

    [<Fact>]
    let ``Task-for CE must call dispose on singleton`` () = async {
        let disposed = ref 0
        let mutable sum = 0
        let values = Gen.getSingletonDisposableTaskSeq disposed

        for x in values do
            sum <- x

        // the DisposeAsync should be called by now
        disposed.Value |> should equal 1
        sum |> should equal 42
    }
