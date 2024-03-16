module TaskSeq.Tests.Concat

open System
open System.Collections.Generic

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.concat - of task seqs
// TaskSeq.concat - of seqs
// TaskSeq.concat - of lists
// TaskSeq.concat - of arrays
// TaskSeq.concat - of resizable arrays
//

let validateSequence ts =
    ts
    |> TaskSeq.toListAsync
    |> Task.map (List.map string)
    |> Task.map (String.concat "")
    |> Task.map (should equal "123456789101234567891012345678910")

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid (taskseq)`` () =
        assertNullArg
        <| fun () -> TaskSeq.concat (null: TaskSeq<TaskSeq<_>>)

    [<Fact>]
    let ``Null source is invalid (seq)`` () =
        assertNullArg
        <| fun () -> TaskSeq.concat (null: TaskSeq<seq<_>>)

    [<Fact>]
    let ``Null source is invalid (array)`` () =
        assertNullArg
        <| fun () -> TaskSeq.concat (null: TaskSeq<array<_>>)

    [<Fact>]
    let ``Null source is invalid (list)`` () =
        assertNullArg
        <| fun () -> TaskSeq.concat (null: TaskSeq<list<_>>)

    [<Fact>]
    let ``Null source is invalid (resizarray)`` () =
        assertNullArg
        <| fun () -> TaskSeq.concat (null: TaskSeq<ResizeArray<_>>)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-concat with nested empty task sequences`` variant =
        taskSeq {
            yield Gen.getEmptyVariant variant
            yield Gen.getEmptyVariant variant
            yield Gen.getEmptyVariant variant
        }
        |> TaskSeq.concat
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-concat with nested empty sequences`` () =
        taskSeq {
            yield Seq.empty<string>
            yield Seq.empty<string>
            yield Seq.empty<string>
        }
        |> TaskSeq.concat
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-concat with nested empty arrays`` () =
        taskSeq {
            yield Array.empty<int>
            yield Array.empty<int>
            yield Array.empty<int>
        }
        |> TaskSeq.concat
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-concat with nested empty lists`` () =
        taskSeq {
            yield List.empty<Guid>
            yield List.empty<Guid>
            yield List.empty<Guid>
        }
        |> TaskSeq.concat
        |> verifyEmpty

    [<Fact>]
    let ``TaskSeq-concat with multiple nested empty resizable arrays`` () =
        taskSeq {
            yield ResizeArray(List.empty<byte>)
            yield ResizeArray(List.empty<byte>)
            yield ResizeArray(List.empty<byte>)
        }
        |> TaskSeq.concat
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-concat with empty source (taskseq)`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.cast<IAsyncEnumerable<int>> // task seq is empty so this should not raise
        |> TaskSeq.concat
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-concat with empty source (seq)`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.cast<int seq> // task seq is empty so this should not raise
        |> TaskSeq.concat
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-concat with empty source (list)`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.cast<int list> // task seq is empty so this should not raise
        |> TaskSeq.concat
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-concat with empty source (array)`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.cast<int[]> // task seq is empty so this should not raise
        |> TaskSeq.concat
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-concat with empty source (resizearray)`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.box
        |> TaskSeq.cast<ResizeArray<int>> // task seq is empty so this should not raise
        |> TaskSeq.concat
        |> verifyEmpty


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-concat with three sequences of sequences`` variant =
        taskSeq {
            yield Gen.getSeqImmutable variant
            yield Gen.getSeqImmutable variant
            yield Gen.getSeqImmutable variant
        }
        |> TaskSeq.concat
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-concat with three sequences of sequences and few empties`` variant =
        taskSeq {
            yield TaskSeq.empty
            yield Gen.getSeqImmutable variant
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield Gen.getSeqImmutable variant
            yield TaskSeq.empty
            yield Gen.getSeqImmutable variant
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield TaskSeq.empty
        }
        |> TaskSeq.concat
        |> validateSequence

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-concat throws when one of inner task sequence is null`` variant =
        fun () ->
            taskSeq {
                yield Gen.getSeqImmutable variant
                yield TaskSeq.empty
                yield null
            }
            |> TaskSeq.concat
            |> consumeTaskSeq
        |> should throwAsyncExact typeof<NullReferenceException>

module SideEffect =
    [<Fact>]
    let ``TaskSeq-concat executes side effects of nested (taskseq)`` () =
        let mutable i = 0

        taskSeq {
            yield Gen.getSeqImmutable SeqImmutable.ThreadSpinWait
            yield Gen.getSeqImmutable SeqImmutable.ThreadSpinWait

            yield taskSeq {
                yield! [ 1..10 ]
                i <- i + 1
            }
        }
        |> TaskSeq.concat
        |> TaskSeq.last // consume
        |> Task.map (fun _ -> i |> should equal 1)

    [<Fact>]
    let ``TaskSeq-concat executes side effects of nested (seq)`` () =
        let mutable i = 0

        taskSeq {
            yield seq { 1..10 }
            yield seq { 1..10 }

            yield seq {
                yield! [ 1..10 ]
                i <- i + 1
            }
        }
        |> TaskSeq.concat
        |> TaskSeq.last // consume
        |> Task.map (fun _ -> i |> should equal 1)

    [<Fact>]
    let ``TaskSeq-concat executes side effects of nested (array)`` () =
        let mutable i = 0

        taskSeq {
            yield [| 1..10 |]
            yield [| 1..10 |]

            yield [| yield! [ 1..10 ]; i <- i + 1 |]
        }
        |> TaskSeq.concat
        |> TaskSeq.last // consume
        |> Task.map (fun _ -> i |> should equal 1)

    [<Fact>]
    let ``TaskSeq-concat executes side effects of nested (list)`` () =
        let mutable i = 0

        taskSeq {
            yield [ 1..10 ]
            yield [ 1..10 ]

            yield [ yield! [ 1..10 ]; i <- i + 1 ]
        }
        |> TaskSeq.concat
        |> TaskSeq.last // consume
        |> Task.map (fun _ -> i |> should equal 1)

    [<Fact>]
    let ``TaskSeq-concat executes side effects of nested (resizearray)`` () =
        let mutable i = 0

        taskSeq {
            yield ResizeArray { 1..10 }
            yield ResizeArray { 1..10 }

            yield
                ResizeArray(
                    seq {
                        yield! [ 1..10 ]
                        i <- i + 1
                    }
                )
        }
        |> TaskSeq.concat
        |> TaskSeq.last // consume
        |> Task.map (fun _ -> i |> should equal 1)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-concat consumes side effects in empty sequences`` variant =
        let mutable i = 0

        taskSeq {
            yield taskSeq { do i <- i + 1 }
            yield Gen.getSeqImmutable variant // not yield-bang!
            yield TaskSeq.empty
            yield taskSeq { do i <- i + 1 }
            yield Gen.getSeqImmutable variant
            yield TaskSeq.empty
            yield Gen.getSeqImmutable variant
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield TaskSeq.empty
            yield taskSeq { do i <- i + 1 }
        }
        |> TaskSeq.concat
        |> validateSequence
        |> Task.map (fun () -> i |> should equal 3)
