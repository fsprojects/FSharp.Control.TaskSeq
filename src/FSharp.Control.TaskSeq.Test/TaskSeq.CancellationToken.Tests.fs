module TaskSeq.Tests.CancellationToken

open System
open System.Threading
open System.Threading.Tasks

open Xunit
open FsUnit.Xunit

open FSharp.Control

/// An infinite taskSeq that yields 1 forever
let private infiniteOnes () = taskSeq {
    while true do
        yield 1
}

/// A finite taskSeq with a few items
let private fiveItems () = taskSeq {
    yield 1
    yield 2
    yield 3
    yield 4
    yield 5
}

module Cancellation =

    [<Fact>]
    let ``GetAsyncEnumerator with pre-cancelled token: first MoveNextAsync throws OperationCanceledException`` () = task {
        use cts = new CancellationTokenSource()
        cts.Cancel()
        use enum = (infiniteOnes ()).GetAsyncEnumerator(cts.Token)

        fun () -> enum.MoveNextAsync().AsTask() |> Task.ignore
        |> should throwAsync typeof<OperationCanceledException>
    }

    [<Fact>]
    let ``GetAsyncEnumerator with pre-cancelled token: MoveNextAsync on finite seq also throws`` () = task {
        use cts = new CancellationTokenSource()
        cts.Cancel()
        use enum = (fiveItems ()).GetAsyncEnumerator(cts.Token)

        fun () -> enum.MoveNextAsync().AsTask() |> Task.ignore
        |> should throwAsync typeof<OperationCanceledException>
    }

    [<Fact>]
    let ``GetAsyncEnumerator with non-cancelled token: iteration proceeds normally`` () = task {
        use cts = new CancellationTokenSource()
        use enum = (fiveItems ()).GetAsyncEnumerator(cts.Token)
        let mutable count = 0
        let mutable canContinue = true

        while canContinue do
            let! hasNext = enum.MoveNextAsync()

            if hasNext then count <- count + 1 else canContinue <- false

        count |> should equal 5
    }

    [<Fact>]
    let ``GetAsyncEnumerator with CancellationToken.None: iteration proceeds normally`` () = task {
        use enum = (fiveItems ()).GetAsyncEnumerator(CancellationToken.None)
        let mutable count = 0
        let mutable canContinue = true

        while canContinue do
            let! hasNext = enum.MoveNextAsync()

            if hasNext then count <- count + 1 else canContinue <- false

        count |> should equal 5
    }

    [<Fact>]
    let ``Token cancelled after partial iteration: next MoveNextAsync throws OperationCanceledException`` () = task {
        use cts = new CancellationTokenSource()
        use enum = (fiveItems ()).GetAsyncEnumerator(cts.Token)

        // Consume first two items normally
        let! _ = enum.MoveNextAsync()
        let! _ = enum.MoveNextAsync()

        // Cancel the token
        cts.Cancel()

        // Next call should throw
        fun () -> enum.MoveNextAsync().AsTask() |> Task.ignore
        |> should throwAsync typeof<OperationCanceledException>
    }

    [<Fact>]
    let ``Infinite sequence with pre-cancelled token: throws immediately without consuming any items`` () = task {
        use cts = new CancellationTokenSource()
        cts.Cancel()
        let mutable itemsConsumed = 0

        let seq = taskSeq {
            while true do
                itemsConsumed <- itemsConsumed + 1
                yield itemsConsumed
        }

        use enum = seq.GetAsyncEnumerator(cts.Token)

        fun () -> enum.MoveNextAsync().AsTask() |> Task.ignore
        |> should throwAsync typeof<OperationCanceledException>

        // The body should not have run (cancellation checked before advancing state machine)
        itemsConsumed |> should equal 0
    }

    [<Fact>]
    let ``Token cancelled mid-iteration of infinite sequence terminates with OperationCanceledException`` () = task {
        use cts = new CancellationTokenSource()
        use enum = (infiniteOnes ()).GetAsyncEnumerator(cts.Token)

        // Iterate a few steps without cancellation
        for _ in 1..5 do
            let! hasNext = enum.MoveNextAsync()
            hasNext |> should be True

        // Now cancel
        cts.Cancel()

        // Next call should throw
        fun () -> enum.MoveNextAsync().AsTask() |> Task.ignore
        |> should throwAsync typeof<OperationCanceledException>
    }

    [<Fact>]
    let ``Multiple enumerators of same sequence respect independent cancellation tokens`` () = task {
        let source = fiveItems ()
        use cts1 = new CancellationTokenSource()
        use cts2 = new CancellationTokenSource()

        // Cancel only the first token
        cts1.Cancel()

        use enum1 = source.GetAsyncEnumerator(cts1.Token)
        use enum2 = source.GetAsyncEnumerator(cts2.Token)

        // enum1 should throw (cancelled)
        fun () -> enum1.MoveNextAsync().AsTask() |> Task.ignore
        |> should throwAsync typeof<OperationCanceledException>

        // enum2 should work normally (not cancelled)
        let! hasNext = enum2.MoveNextAsync()
        hasNext |> should be True
        enum2.Current |> should equal 1
    }
