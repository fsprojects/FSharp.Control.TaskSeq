module FSharpy.TaskSeq.Tests.``Conversion-To``

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
    let tq = createDummyTaskSeq 10
    let! (results: _[]) = tq |> TaskSeq.toArrayAsync
    results |> should equal [| 0..9 |]
}

[<Fact>]
let ``TaskSeq-toListAsync should succeed`` () = task {
    let tq = createDummyTaskSeq 10
    let! (results: list<_>) = tq |> TaskSeq.toListAsync
    results |> should equal [ 0..9 ]
}

[<Fact>]
let ``TaskSeq-toSeqCachedAsync should succeed`` () = task {
    let tq = createDummyTaskSeq 10
    let! (results: seq<_>) = tq |> TaskSeq.toSeqCachedAsync
    results |> Seq.toArray |> should equal [| 0..9 |]
}

[<Fact>]
let ``TaskSeq-toIListAsync should succeed`` () = task {
    let tq = createDummyTaskSeq 10
    let! (results: IList<_>) = tq |> TaskSeq.toIListAsync
    results |> Seq.toArray |> should equal [| 0..9 |]
}

[<Fact>]
let ``TaskSeq-toResizeArray should succeed`` () = task {
    let tq = createDummyTaskSeq 10
    let! (results: ResizeArray<_>) = tq |> TaskSeq.toResizeArrayAsync
    results |> should equal [| 0..9 |]
}

[<Fact>]
let ``TaskSeq-toArray should succeed and be blocking`` () =
    let tq = createDummyTaskSeq 10
    let (results: _[]) = tq |> TaskSeq.toArray
    results |> should equal [| 0..9 |]

[<Fact>]
let ``TaskSeq-toList should succeed and be blocking`` () =
    let tq = createDummyTaskSeq 10
    let (results: list<_>) = tq |> TaskSeq.toList
    results |> should equal [ 0..9 ]

[<Fact>]
let ``TaskSeq-toSeqCached should succeed and be blocking`` () =
    let tq = createDummyTaskSeq 10
    let (results: seq<_>) = tq |> TaskSeq.toSeqCached
    results |> Seq.toArray |> should equal [| 0..9 |]
