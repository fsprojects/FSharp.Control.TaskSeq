module TaskSeq.Tests.``Conversion-From``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

let validateSequence sq =
    TaskSeq.toArrayAsync sq
    |> Task.map (Seq.toArray >> should equal [| 0..9 |])

module EmptySeq =
    [<Fact>]
    let ``TaskSeq-ofAsyncArray with empty set`` () =
        Array.init 0 (fun x -> async { return x })
        |> TaskSeq.ofAsyncArray
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-ofAsyncList with empty set`` () =
        List.init 0 (fun x -> async { return x })
        |> TaskSeq.ofAsyncList
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-ofAsyncSeq with empty set`` () =
        Seq.init 0 (fun x -> async { return x })
        |> TaskSeq.ofAsyncSeq
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-ofTaskArray with empty set`` () =
        Array.init 0 (fun x -> task { return x })
        |> TaskSeq.ofTaskArray
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-ofTaskList with empty set`` () =
        List.init 0 (fun x -> task { return x })
        |> TaskSeq.ofTaskList
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-ofTaskSeq with empty set`` () =
        Seq.init 0 (fun x -> task { return x })
        |> TaskSeq.ofTaskSeq
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-ofResizeArray with empty set`` () = ResizeArray() |> TaskSeq.ofResizeArray |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-ofArray with empty set`` () = Array.init 0 id |> TaskSeq.ofArray |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-ofList with empty set`` () = List.init 0 id |> TaskSeq.ofList |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-ofSeq with empty set`` () = Seq.init 0 id |> TaskSeq.ofSeq |> verifyEmpty


module Immutable =
    [<Fact>]
    let ``TaskSeq-ofAsyncArray should succeed`` () =
        Array.init 10 (fun x -> async { return x })
        |> TaskSeq.ofAsyncArray
        |> validateSequence

    [<Fact>]
    let ``TaskSeq-ofAsyncList should succeed`` () =
        List.init 10 (fun x -> async { return x })
        |> TaskSeq.ofAsyncList
        |> validateSequence

    [<Fact>]
    let ``TaskSeq-ofAsyncSeq should succeed`` () =
        Seq.init 10 (fun x -> async { return x })
        |> TaskSeq.ofAsyncSeq
        |> validateSequence

    [<Fact>]
    let ``TaskSeq-ofTaskArray should succeed`` () =
        Array.init 10 (fun x -> task { return x })
        |> TaskSeq.ofTaskArray
        |> validateSequence

    [<Fact>]
    let ``TaskSeq-ofTaskList should succeed`` () =
        List.init 10 (fun x -> task { return x })
        |> TaskSeq.ofTaskList
        |> validateSequence

    [<Fact>]
    let ``TaskSeq-ofTaskSeq should succeed`` () =
        Seq.init 10 (fun x -> task { return x })
        |> TaskSeq.ofTaskSeq
        |> validateSequence

    [<Fact>]
    let ``TaskSeq-ofResizeArray should succeed`` () =
        ResizeArray [ 0..9 ]
        |> TaskSeq.ofResizeArray
        |> validateSequence

    [<Fact>]
    let ``TaskSeq-ofArray should succeed`` () = Array.init 10 id |> TaskSeq.ofArray |> validateSequence

    [<Fact>]
    let ``TaskSeq-ofList should succeed`` () = List.init 10 id |> TaskSeq.ofList |> validateSequence

    [<Fact>]
    let ``TaskSeq-ofSeq should succeed`` () = Seq.init 10 id |> TaskSeq.ofSeq |> validateSequence
