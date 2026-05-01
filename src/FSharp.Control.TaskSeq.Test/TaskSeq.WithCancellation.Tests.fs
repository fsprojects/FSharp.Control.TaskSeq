module TaskSeq.Tests.``WithCancellation``

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Xunit
open FsUnit.Xunit

open FSharp.Control

/// A simple IAsyncEnumerable whose GetAsyncEnumerator records the token it was called with.
type TokenCapturingSeq<'T>(items: 'T list) =
    let mutable capturedToken = CancellationToken.None

    member _.CapturedToken = capturedToken

    interface IAsyncEnumerable<'T> with
        member _.GetAsyncEnumerator(ct) =
            capturedToken <- ct

            let source = taskSeq {
                for x in items do
                    yield x
            }

            source.GetAsyncEnumerator(ct)

module ``Null check`` =

    [<Fact>]
    let ``TaskSeq-withCancellation: null source throws ArgumentNullException`` () =
        assertNullArg
        <| fun () -> TaskSeq.withCancellation CancellationToken.None null

module ``Token threading`` =

    [<Fact>]
    let ``TaskSeq-withCancellation: passes supplied token to GetAsyncEnumerator`` () = task {
        let source = TokenCapturingSeq([ 1; 2; 3 ])
        use cts = new CancellationTokenSource()

        let wrapped = TaskSeq.withCancellation cts.Token (source :> IAsyncEnumerable<_>)
        let! _ = TaskSeq.toArrayAsync wrapped
        source.CapturedToken |> should equal cts.Token
    }

    [<Fact>]
    let ``TaskSeq-withCancellation: overrides any token passed to GetAsyncEnumerator`` () = task {
        let source = TokenCapturingSeq([ 1; 2; 3 ])
        use cts = new CancellationTokenSource()

        let wrapped = TaskSeq.withCancellation cts.Token (source :> IAsyncEnumerable<_>)

        // Consume with a different token; withCancellation should win
        use outerCts = new CancellationTokenSource()
        let enum = wrapped.GetAsyncEnumerator(outerCts.Token)

        while! enum.MoveNextAsync() do
            ()

        source.CapturedToken |> should equal cts.Token
    }

    [<Fact>]
    let ``TaskSeq-withCancellation: CancellationToken.None passes through correctly`` () = task {
        let source = TokenCapturingSeq([ 10; 20 ])

        let wrapped = TaskSeq.withCancellation CancellationToken.None (source :> IAsyncEnumerable<_>)
        let! _ = TaskSeq.toArrayAsync wrapped
        source.CapturedToken |> should equal CancellationToken.None
    }

module ``Cancellation behaviour`` =

    [<Fact>]
    let ``TaskSeq-withCancellation: pre-cancelled token causes OperationCanceledException on iteration`` () = task {
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let source = taskSeq {
            while true do
                yield 1
        }

        let wrapped = TaskSeq.withCancellation cts.Token source

        fun () -> TaskSeq.iter ignore wrapped |> Task.ignore
        |> should throwAsync typeof<OperationCanceledException>
    }

    [<Fact>]
    let ``TaskSeq-withCancellation: token cancelled mid-iteration raises OperationCanceledException`` () = task {
        use cts = new CancellationTokenSource()

        let source = taskSeq {
            for i in 1..100 do
                yield i
        }

        let wrapped = TaskSeq.withCancellation cts.Token source

        fun () ->
            task {
                let mutable count = 0
                use enum = wrapped.GetAsyncEnumerator(CancellationToken.None)

                while! enum.MoveNextAsync() do
                    count <- count + 1

                    if count = 3 then
                        cts.Cancel()
            }
            |> Task.ignore
        |> should throwAsync typeof<OperationCanceledException>
    }

module ``Sequence contents`` =

    [<Fact>]
    let ``TaskSeq-withCancellation: empty source produces empty sequence`` () =
        TaskSeq.empty<int>
        |> TaskSeq.withCancellation CancellationToken.None
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-withCancellation: finite source produces all items`` () = task {
        let! result =
            taskSeq {
                for i in 1..10 do
                    yield i
            }
            |> TaskSeq.withCancellation CancellationToken.None
            |> TaskSeq.toArrayAsync

        result |> should equal [| 1..10 |]
    }

    [<Fact>]
    let ``TaskSeq-withCancellation: can be used with TaskSeq combinators`` () = task {
        use cts = new CancellationTokenSource()

        let! result =
            taskSeq {
                for i in 1..5 do
                    yield i
            }
            |> TaskSeq.withCancellation cts.Token
            |> TaskSeq.map (fun x -> x * 2)
            |> TaskSeq.toArrayAsync

        result |> should equal [| 2; 4; 6; 8; 10 |]
    }

    [<Fact>]
    let ``TaskSeq-withCancellation: can be piped like .WithCancellation usage pattern`` () = task {
        use cts = new CancellationTokenSource()
        let mutable collected = ResizeArray()

        let source = taskSeq {
            for i in 1..5 do
                yield i
        }

        do!
            source
            |> TaskSeq.withCancellation cts.Token
            |> TaskSeq.iterAsync (fun x -> task { collected.Add(x) })

        collected |> Seq.toArray |> should equal [| 1..5 |]
    }

module SideEffects =

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-withCancellation applied multiple times`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let wrapped = TaskSeq.withCancellation CancellationToken.None ts

        let! first = wrapped |> TaskSeq.toArrayAsync
        let! second = wrapped |> TaskSeq.toArrayAsync
        let! third = wrapped |> TaskSeq.toArrayAsync

        first |> should equal [| 1..10 |]
        second |> should equal [| 11..20 |]
        third |> should equal [| 21..30 |]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-withCancellation with active CancellationToken applied multiple times`` variant = task {
        use cts = new CancellationTokenSource()
        let ts = Gen.getSeqWithSideEffect variant
        let wrapped = TaskSeq.withCancellation cts.Token ts

        let! first = wrapped |> TaskSeq.toArrayAsync
        let! second = wrapped |> TaskSeq.toArrayAsync

        first |> should equal [| 1..10 |]
        second |> should equal [| 11..20 |]
    }

    [<Fact>]
    let ``TaskSeq-withCancellation evaluates each source element exactly once per iteration`` () = task {
        let mutable count = 0

        let ts = taskSeq {
            for i in 1..5 do
                count <- count + 1
                yield i
        }

        let! _ =
            ts
            |> TaskSeq.withCancellation CancellationToken.None
            |> TaskSeq.toArrayAsync

        count |> should equal 5

        let! _ =
            ts
            |> TaskSeq.withCancellation CancellationToken.None
            |> TaskSeq.toArrayAsync

        count |> should equal 10
    }
