module TaskSeq.Tests.``Conversion-To``

open System.Collections.Generic
open System.Threading.Channels

open Xunit
open FsUnit.Xunit

open FSharp.Control

////////////////////////////////////////////////////////////////////////////
///                                                                      ///
/// Notes for contributors:                                              ///
///                                                                      ///
/// Conversion functions are expected to return a certain type           ///
/// To prevent accidental change of signature, and because most          ///
/// sequence-like functions can succeed tests interchangeably with       ///
/// different sequence-like signatures, these tests                      ///
/// deliberately have a type annotation to prevent accidental changing   ///
/// of the surface-area signatures.                                      ///
///                                                                      ///
////////////////////////////////////////////////////////////////////////////

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-toArrayAsync with empty`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let! (results: _[]) = tq |> TaskSeq.toArrayAsync
        results |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-toListAsync with empty`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let! (results: list<_>) = tq |> TaskSeq.toListAsync
        results |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-toIListAsync with empty`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let! (results: IList<_>) = tq |> TaskSeq.toIListAsync
        results |> Seq.toArray |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-toResizeArray with empty`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let! (results: ResizeArray<_>) = tq |> TaskSeq.toResizeArrayAsync
        results |> Seq.toArray |> should be Empty
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-toArray with empty`` variant =
        let tq = Gen.getEmptyVariant variant
        let (results: _[]) = tq |> TaskSeq.toArray
        results |> should be Empty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-toList with empty`` variant =
        let tq = Gen.getEmptyVariant variant
        let (results: list<_>) = tq |> TaskSeq.toList
        results |> should be Empty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-toSeqCached with empty`` variant =
        let tq = Gen.getEmptyVariant variant
        let (results: seq<_>) = tq |> TaskSeq.toSeq
        results |> Seq.toArray |> should be Empty

module Immutable =

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-toArrayAsync should succeed`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let! (results: _[]) = tq |> TaskSeq.toArrayAsync
        results |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-toListAsync should succeed`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let! (results: list<_>) = tq |> TaskSeq.toListAsync
        results |> should equal [ 1..10 ]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-toIListAsync should succeed`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let! (results: IList<_>) = tq |> TaskSeq.toIListAsync
        results |> Seq.toArray |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-toResizeArray should succeed`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let! (results: ResizeArray<_>) = tq |> TaskSeq.toResizeArrayAsync
        results |> Seq.toArray |> should equal [| 1..10 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-toArray should succeed and be blocking`` variant =
        let tq = Gen.getSeqImmutable variant
        let (results: _[]) = tq |> TaskSeq.toArray
        results |> should equal [| 1..10 |]

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-toList should succeed and be blocking`` variant =
        let tq = Gen.getSeqImmutable variant
        let (results: list<_>) = tq |> TaskSeq.toList
        results |> should equal [ 1..10 ]

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-toSeqCached should succeed and be blocking`` variant =
        let tq = Gen.getSeqImmutable variant
        let (results: seq<_>) = tq |> TaskSeq.toSeq
        results |> Seq.toArray |> should equal [| 1..10 |]


module SideEffects =

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-toArrayAsync should execute side effects multiple times`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let! (results1: _[]) = tq |> TaskSeq.toArrayAsync
        let! (results2: _[]) = tq |> TaskSeq.toArrayAsync
        results1 |> should equal [| 1..10 |]
        results2 |> should equal [| 11..20 |]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-toArrayAsync can be applied multiple times to the same sequence`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let! (results1: _[]) = tq |> TaskSeq.toArrayAsync
        let! (results2: _[]) = tq |> TaskSeq.toArrayAsync
        results1 |> should equal [| 1..10 |]
        results2 |> should equal [| 11..20 |]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-toListAsync should execute side effects multiple times`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let! (results1: list<_>) = tq |> TaskSeq.toListAsync
        let! (results2: list<_>) = tq |> TaskSeq.toListAsync
        results1 |> should equal [ 1..10 ]
        results2 |> should equal [ 11..20 ]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-toIListAsync should execute side effects multiple times`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let! (results1: IList<_>) = tq |> TaskSeq.toIListAsync
        let! (results2: IList<_>) = tq |> TaskSeq.toIListAsync
        results1 |> Seq.toArray |> should equal [| 1..10 |]
        results2 |> Seq.toArray |> should equal [| 11..20 |]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-toResizeArray should execute side effects multiple times`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let! (results1: ResizeArray<_>) = tq |> TaskSeq.toResizeArrayAsync
        let! (results2: ResizeArray<_>) = tq |> TaskSeq.toResizeArrayAsync
        results1 |> Seq.toArray |> should equal [| 1..10 |]
        results2 |> Seq.toArray |> should equal [| 11..20 |]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-toArray should execute side effects multiple times`` variant =
        let tq = Gen.getSeqWithSideEffect variant
        let (results1: _[]) = tq |> TaskSeq.toArray
        let (results2: _[]) = tq |> TaskSeq.toArray
        results1 |> should equal [| 1..10 |]
        results2 |> should equal [| 11..20 |]

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-toList should execute side effects multiple times`` variant =
        let tq = Gen.getSeqWithSideEffect variant
        let (results1: list<_>) = tq |> TaskSeq.toList
        let (results2: list<_>) = tq |> TaskSeq.toList
        results1 |> should equal [ 1..10 ]
        results2 |> should equal [ 11..20 ]

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-toSeqCached should execute side effects multiple times`` variant =
        let tq = Gen.getSeqWithSideEffect variant
        let (results1: seq<_>) = tq |> TaskSeq.toSeq
        let (results2: seq<_>) = tq |> TaskSeq.toSeq
        results1 |> Seq.toArray |> should equal [| 1..10 |]
        results2 |> Seq.toArray |> should equal [| 11..20 |]

module Channel =

    [<Fact>]
    let ``TaskSeq-toChannelAsync with null writer raises`` () =
        assertNullArg
        <| fun () ->
            TaskSeq.toChannelAsync null (TaskSeq.ofArray [| 1 |])
            |> ignore

    [<Fact>]
    let ``TaskSeq-toChannelAsync with null source raises`` () =
        let ch = Channel.CreateUnbounded<int>()

        assertNullArg
        <| fun () -> TaskSeq.toChannelAsync ch.Writer null |> ignore

    [<Fact>]
    let ``TaskSeq-ofChannel with null reader raises`` () =
        assertNullArg
        <| fun () -> TaskSeq.ofChannel<int> null |> ignore

    [<Fact>]
    let ``TaskSeq-toChannelAsync with empty source completes the channel`` () = task {
        let ch = Channel.CreateUnbounded<int>()
        do! TaskSeq.toChannelAsync ch.Writer TaskSeq.empty
        ch.Reader.Completion.IsCompleted |> should be True
        let! results = TaskSeq.ofChannel ch.Reader |> TaskSeq.toArrayAsync
        results |> should be Empty
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-toChannelAsync writes all elements and completes the channel`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let ch = Channel.CreateUnbounded<int>()
        do! TaskSeq.toChannelAsync ch.Writer tq
        let! results = TaskSeq.ofChannel ch.Reader |> TaskSeq.toArrayAsync
        results |> should equal [| 1..10 |]
        // Completion resolves once the channel is marked done and the buffer is drained
        do! ch.Reader.Completion
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-ofChannel yields all elements written to the channel`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let ch = Channel.CreateUnbounded<int>()
        do! TaskSeq.toChannelAsync ch.Writer tq
        let! results = TaskSeq.ofChannel ch.Reader |> TaskSeq.toArrayAsync
        results |> should equal [| 1..10 |]
    }

    [<Fact>]
    let ``TaskSeq-ofChannel ends when channel is completed and drained`` () = task {
        let ch = Channel.CreateUnbounded<int>()
        do! ch.Writer.WriteAsync 42
        do! ch.Writer.WriteAsync 99
        ch.Writer.Complete()
        let! results = TaskSeq.ofChannel ch.Reader |> TaskSeq.toArrayAsync
        results |> should equal [| 42; 99 |]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-toChannelAsync executes side effects`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let ch = Channel.CreateUnbounded<int>()
        do! TaskSeq.toChannelAsync ch.Writer tq
        let! results = TaskSeq.ofChannel ch.Reader |> TaskSeq.toArrayAsync
        results |> should equal [| 1..10 |]
    }
