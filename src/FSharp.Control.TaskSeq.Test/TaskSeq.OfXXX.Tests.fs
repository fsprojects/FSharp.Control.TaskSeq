module TaskSeq.Tests.``Conversion-From``

open Xunit
open FsUnit.Xunit

open FSharp.Control

let validateSequence sq =
    TaskSeq.toArrayAsync sq
    |> Task.map (Seq.toArray >> should equal [| 0..9 |])

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        // note: ofList and its variants do not have null as proper value
        assertNullArg <| fun () -> TaskSeq.ofAsyncArray null
        assertNullArg <| fun () -> TaskSeq.ofAsyncSeq null
        assertNullArg <| fun () -> TaskSeq.ofTaskArray null
        assertNullArg <| fun () -> TaskSeq.ofTaskSeq null
        assertNullArg <| fun () -> TaskSeq.ofResizeArray null
        assertNullArg <| fun () -> TaskSeq.ofArray null
        assertNullArg <| fun () -> TaskSeq.ofSeq null

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

module SideEffects =
    [<Fact>]
    let ``ofSeq re-evaluates the underlying source seq on each re-enumeration`` () = task {
        let mutable count = 0

        // a lazy IEnumerable — each GetEnumerator() call re-executes the body
        let lazySeq = seq {
            for i in 1..3 do
                count <- count + 1
                yield i
        }

        let ts = TaskSeq.ofSeq lazySeq
        let! arr1 = ts |> TaskSeq.toArrayAsync
        // each item triggered the side effect once
        count |> should equal 3

        let! arr2 = ts |> TaskSeq.toArrayAsync
        // the underlying seq is re-traversed on the second GetAsyncEnumerator call
        count |> should equal 6
        arr1 |> should equal arr2
    }

    [<Fact>]
    let ``ofTaskSeq with lazy seq of tasks re-creates tasks on each re-enumeration`` () = task {
        let mutable count = 0

        // a lazy IEnumerable of Task objects — each seq iteration creates fresh Task objects
        let lazyTaskSeq = seq {
            for i in 1..3 do
                yield task {
                    count <- count + 1
                    return i
                }
        }

        let ts = TaskSeq.ofTaskSeq lazyTaskSeq
        let! arr1 = ts |> TaskSeq.toArrayAsync
        count |> should equal 3

        let! arr2 = ts |> TaskSeq.toArrayAsync
        // the underlying seq is re-iterated; new Task objects are created and run
        count |> should equal 6
        arr1 |> should equal arr2
    }

    [<Fact>]
    let ``ofTaskArray does not re-run tasks on re-enumeration; task results are cached`` () = task {
        let mutable count = 0

        // tasks are created upfront; they run synchronously to completion when constructed
        let tasks =
            Array.init 3 (fun i -> task {
                count <- count + 1
                return i + 1
            })

        // all three tasks have already completed synchronously
        count |> should equal 3

        let ts = TaskSeq.ofTaskArray tasks
        let! arr1 = ts |> TaskSeq.toArrayAsync

        // awaiting already-completed tasks does not re-run them
        count |> should equal 3
        arr1 |> should equal [| 1; 2; 3 |]

        let! arr2 = ts |> TaskSeq.toArrayAsync
        // the second enumeration re-awaits the same cached task results
        count |> should equal 3
        arr2 |> should equal arr1
    }
