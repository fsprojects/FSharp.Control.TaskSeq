module FSharpy.Tests.Zip

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

[<Fact>]
let ``TaskSeq-zip zips in correct order`` () = task {
    let one = createDummyTaskSeq 10
    let two = createDummyTaskSeq 10
    let combined = TaskSeq.zip one two
    let! combined = TaskSeq.toArrayAsync combined

    combined
    |> Array.forall (fun (x, y) -> x = y)
    |> should be True

    combined |> should be (haveLength 10)

    combined
    |> should equal (Array.init 10 (fun x -> x + 1, x + 1))
}

[<Fact>]
let ``TaskSeq-zip zips in correct order for differently delayed sequences`` () = task {
    let one = createDummyDirectTaskSeq 10
    let two = createDummyTaskSeq 10
    let combined = TaskSeq.zip one two
    let! combined = TaskSeq.toArrayAsync combined

    combined
    |> Array.forall (fun (x, y) -> x = y)
    |> should be True

    combined |> should be (haveLength 10)

    combined
    |> should equal (Array.init 10 (fun x -> x + 1, x + 1))
}

[<Theory; InlineData 100; InlineData 1_000; InlineData 10_000; InlineData 100_000>]
let ``TaskSeq-zip zips large sequences just fine`` length = task {
    let one = createDummyTaskSeqWith 10L<µs> 50L<µs> length
    let two = createDummyDirectTaskSeq length
    let combined = TaskSeq.zip one two
    let! combined = TaskSeq.toArrayAsync combined

    combined
    |> Array.forall (fun (x, y) -> x = y)
    |> should be True

    combined |> should be (haveLength length)
    combined |> Array.last |> should equal (length, length)
}

[<Fact>]
let ``TaskSeq-zip zips different types`` () = task {
    let one = taskSeq {
        yield "one"
        yield "two"
    }

    let two = taskSeq {
        yield 42L
        yield 43L
    }

    let combined = TaskSeq.zip one two
    let! combined = TaskSeq.toArrayAsync combined

    combined |> should equal [| ("one", 42L); ("two", 43L) |]
}

[<Fact>]
let ``TaskSeq-zip throws on unequal lengths`` () = task {
    let one = createDummyTaskSeq 10
    let two = createDummyTaskSeq 11
    let combined = TaskSeq.zip one two

    fun () -> TaskSeq.toArrayAsync combined |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact>]
let ``TaskSeq-zip can zip empty arrays`` () = task {
    let combined = TaskSeq.zip TaskSeq.empty<int> TaskSeq.empty<string>
    let! combined = TaskSeq.toArrayAsync combined
    combined |> should be Empty
    Array.isEmpty combined |> should be True
}
