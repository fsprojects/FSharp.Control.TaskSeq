module FSharpy.Tests.``Conversion-To``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy
open System.Collections.Generic

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

[<Fact>]
let ``TaskSeq-toArrayAsync should succeed`` () = task {
    let tq = Gen.sideEffectTaskSeq 10
    let! (results: _[]) = tq |> TaskSeq.toArrayAsync
    results |> should equal [| 1..10 |]
}

[<Fact>]
let ``TaskSeq-toArrayAsync can be applied multiple times to the same sequence`` () = task {
    let tq = Gen.sideEffectTaskSeq 10
    let! (results1: _[]) = tq |> TaskSeq.toArrayAsync
    let! (results2: _[]) = tq |> TaskSeq.toArrayAsync
    let! (results3: _[]) = tq |> TaskSeq.toArrayAsync
    let! (results4: _[]) = tq |> TaskSeq.toArrayAsync
    results1 |> should equal [| 1..10 |]
    results2 |> should equal [| 11..20 |]
    results3 |> should equal [| 21..30 |]
    results4 |> should equal [| 31..40 |]
}

[<Fact>]
let ``TaskSeq-toListAsync should succeed`` () = task {
    let tq = Gen.sideEffectTaskSeq 10
    let! (results: list<_>) = tq |> TaskSeq.toListAsync
    results |> should equal [ 1..10 ]
}

[<Fact>]
let ``TaskSeq-toSeqCachedAsync should succeed`` () = task {
    let tq = Gen.sideEffectTaskSeq 10
    let! (results: seq<_>) = tq |> TaskSeq.toSeqCachedAsync
    results |> Seq.toArray |> should equal [| 1..10 |]
}

[<Fact>]
let ``TaskSeq-toIListAsync should succeed`` () = task {
    let tq = Gen.sideEffectTaskSeq 10
    let! (results: IList<_>) = tq |> TaskSeq.toIListAsync
    results |> Seq.toArray |> should equal [| 1..10 |]
}

[<Fact>]
let ``TaskSeq-toResizeArray should succeed`` () = task {
    let tq = Gen.sideEffectTaskSeq 10
    let! (results: ResizeArray<_>) = tq |> TaskSeq.toResizeArrayAsync
    results |> Seq.toArray |> should equal [| 1..10 |]
}

[<Fact>]
let ``TaskSeq-toArray should succeed and be blocking`` () =
    let tq = Gen.sideEffectTaskSeq 10
    let (results: _[]) = tq |> TaskSeq.toArray
    results |> should equal [| 1..10 |]

[<Fact>]
let ``TaskSeq-toList should succeed and be blocking`` () =
    let tq = Gen.sideEffectTaskSeq 10
    let (results: list<_>) = tq |> TaskSeq.toList
    results |> should equal [ 1..10 ]

[<Fact>]
let ``TaskSeq-toSeqCached should succeed and be blocking`` () =
    let tq = Gen.sideEffectTaskSeq 10
    let (results: seq<_>) = tq |> TaskSeq.toSeqCached
    results |> Seq.toArray |> should equal [| 1..10 |]
