module TaskSeq.Tests.Except

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.except
// TaskSeq.exceptOfSeq
//


module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg <| fun () -> TaskSeq.except null TaskSeq.empty
        assertNullArg <| fun () -> TaskSeq.except TaskSeq.empty null
        assertNullArg <| fun () -> TaskSeq.except null null

        assertNullArg
        <| fun () -> TaskSeq.exceptOfSeq null TaskSeq.empty

        assertNullArg
        <| fun () -> TaskSeq.exceptOfSeq Seq.empty null

        assertNullArg <| fun () -> TaskSeq.exceptOfSeq null null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-except`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.except (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-exceptOfSeq`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.exceptOfSeq Seq.empty
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-except v2`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.except TaskSeq.empty
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-except v3`` variant =
        TaskSeq.empty
        |> TaskSeq.except (Gen.getEmptyVariant variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-except no side effect in exclude seq if source seq is empty`` variant =
        // TODO use variant
        let mutable i = 0

        let exclude = taskSeq {
            i <- i + 1
            yield 12
        }

        TaskSeq.empty
        |> TaskSeq.except exclude
        |> verifyEmpty
        |> Task.map (fun () -> i |> should equal 0) // exclude seq is only enumerated after first item in source

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-except removes duplicates`` variant =
        TaskSeq.ofList [ 1; 1; 2; 3; 4; 12; 12; 12; 13; 13; 13; 13; 13; 99 ]
        |> TaskSeq.except (Gen.getSeqImmutable variant)
        |> TaskSeq.toArrayAsync
        |> Task.map (should equal [| 12; 13; 99 |])

    [<Fact>]
    let ``TaskSeq-except removes duplicates with empty itemsToExcept`` () =
        TaskSeq.ofList [ 1; 1; 2; 3; 4; 12; 12; 12; 13; 13; 13; 13; 13; 99 ]
        |> TaskSeq.except TaskSeq.empty
        |> TaskSeq.toArrayAsync
        |> Task.map (should equal [| 1; 2; 3; 4; 12; 13; 99 |])

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-except removes everything`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.except (Gen.getSeqImmutable variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-except removes everything with duplicates`` variant =
        taskSeq {
            yield! Gen.getSeqImmutable variant
            yield! Gen.getSeqImmutable variant
            yield! Gen.getSeqImmutable variant
            yield! Gen.getSeqImmutable variant
        }
        |> TaskSeq.except (Gen.getSeqImmutable variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exceptOfSeq removes duplicates`` variant =
        // TODO use variant
        TaskSeq.ofList [ 1; 1; 2; 3; 4; 12; 12; 12; 13; 13; 13; 13; 13; 99 ]
        |> TaskSeq.exceptOfSeq [ 1..10 ]
        |> TaskSeq.toArrayAsync
        |> Task.map (should equal [| 12; 13; 99 |])

    [<Fact>]
    let ``TaskSeq-exceptOfSeq removes duplicates with empty itemsToExcept`` () =
        TaskSeq.ofList [ 1; 1; 2; 3; 4; 12; 12; 12; 13; 13; 13; 13; 13; 99 ]
        |> TaskSeq.exceptOfSeq Seq.empty
        |> TaskSeq.toArrayAsync
        |> Task.map (should equal [| 1; 2; 3; 4; 12; 13; 99 |])

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exceptOfSeq removes everything`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.exceptOfSeq [ 1..10 ]
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exceptOfSeq removes everything with duplicates`` variant =
        taskSeq {
            yield! Gen.getSeqImmutable variant
            yield! Gen.getSeqImmutable variant
            yield! Gen.getSeqImmutable variant
            yield! Gen.getSeqImmutable variant
        }
        |> TaskSeq.exceptOfSeq [ 1..10 ]
        |> verifyEmpty

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-except removes duplicates`` variant =
        TaskSeq.ofList [ 1; 1; 2; 3; 4; 12; 12; 12; 13; 13; 13; 13; 13; 99 ]
        |> TaskSeq.except (Gen.getSeqWithSideEffect variant)
        |> TaskSeq.toArrayAsync
        |> Task.map (should equal [| 12; 13; 99 |])

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-except removes everything`` variant =
        Gen.getSeqWithSideEffect variant
        |> TaskSeq.except (Gen.getSeqWithSideEffect variant)
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-except removes everything with duplicates`` variant =
        taskSeq {
            yield! Gen.getSeqWithSideEffect variant
            yield! Gen.getSeqWithSideEffect variant
            yield! Gen.getSeqWithSideEffect variant
            yield! Gen.getSeqWithSideEffect variant
        }
        |> TaskSeq.except (Gen.getSeqWithSideEffect variant)
        |> verifyEmpty
