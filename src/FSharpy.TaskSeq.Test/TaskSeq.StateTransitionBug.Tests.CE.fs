module FSharpy.Tests.``State transition bug and InvalidState``

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy
open System.Threading.Tasks
open System.Diagnostics
open System.Collections.Generic

let getEmptyVariant variant : IAsyncEnumerable<int> =
    match variant with
    | "do" -> taskSeq { do ignore () }
    | "do!" -> taskSeq { do! task { return () } } // TODO: this doesn't work with Task, only Task<unit>...
    | "yield! (seq)" -> taskSeq { yield! Seq.empty<int> }
    | "yield! (taskseq)" -> taskSeq { yield! taskSeq { do ignore () } }
    | _ -> failwith "Uncovered variant of test"


[<Fact>]
let ``CE empty taskSeq with MoveNextAsync -- untyped`` () = task {
    let tskSeq = taskSeq { do ignore () }

    Assert.IsAssignableFrom<IAsyncEnumerable<obj>>(tskSeq)
    |> ignore

    let! noNext = tskSeq.GetAsyncEnumerator().MoveNextAsync()
    noNext |> should be False
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE empty taskSeq with MoveNextAsync -- typed`` variant = task {
    let tskSeq = getEmptyVariant variant

    Assert.IsAssignableFrom<IAsyncEnumerable<int>>(tskSeq)
    |> ignore

    let! noNext = tskSeq.GetAsyncEnumerator().MoveNextAsync()
    noNext |> should be False
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE  empty taskSeq, GetAsyncEnumerator multiple times`` variant = task {
    let tskSeq = getEmptyVariant variant
    use enumerator = tskSeq.GetAsyncEnumerator()
    use enumerator = tskSeq.GetAsyncEnumerator()
    use enumerator = tskSeq.GetAsyncEnumerator()
    ()
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE  empty taskSeq, GetAsyncEnumerator multiple times and then MoveNextAsync`` variant = task {
    let tskSeq = getEmptyVariant variant
    use enumerator = tskSeq.GetAsyncEnumerator()
    use enumerator = tskSeq.GetAsyncEnumerator()
    let! isNext = enumerator.MoveNextAsync()
    ()
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE empty taskSeq, GetAsyncEnumerator + MoveNextAsync multiple times`` variant = task {
    let tskSeq = getEmptyVariant variant
    use enumerator = tskSeq.GetAsyncEnumerator()
    let! isNext = enumerator.MoveNextAsync()
    use enumerator = tskSeq.GetAsyncEnumerator()
    let! isNext = enumerator.MoveNextAsync()
    ()
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE  empty taskSeq, GetAsyncEnumerator + MoveNextAsync in a loop`` variant = task {
    let tskSeq = getEmptyVariant variant

    // let's get the enumerator a few times
    for i in 0..10 do
        printfn "Calling GetAsyncEnumerator for the #%i time" i
        use enumerator = tskSeq.GetAsyncEnumerator()
        let! isNext = enumerator.MoveNextAsync()
        isNext |> should be False
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE taskSeq with two items, MoveNext once too far`` variant = task {
    let tskSeq = taskSeq {
        yield 1
        yield 2
    }

    let enum = tskSeq.GetAsyncEnumerator()
    let! isNext = enum.MoveNextAsync() // true
    let! isNext = enum.MoveNextAsync() // true
    let! isNext = enum.MoveNextAsync() // false
    let! isNext = enum.MoveNextAsync() // error here, see
    ()
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE taskSeq with two items, MoveNext too far`` variant = task {
    let tskSeq = taskSeq {
        yield 1
        yield 2
    }

    // let's call MoveNext multiple times on an empty sequence
    let enum = tskSeq.GetAsyncEnumerator()

    for i in 0..10 do
        printfn "Calling MoveNext for the #%i time" i
        let! isNext = enum.MoveNextAsync()
        //isNext |> should be False
        ()
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE taskSeq with two items, multiple TaskSeq.map`` variant = task {
    let tskSeq = taskSeq {
        yield 1
        yield 2
    }

    // let's call MoveNext multiple times on an empty sequence
    let ts1 = tskSeq |> TaskSeq.map (fun i -> i + 1)
    let result1 = TaskSeq.toArray ts1
    let ts2 = ts1 |> TaskSeq.map (fun i -> i + 1)
    let result2 = TaskSeq.toArray ts2
    ()
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE taskSeq with two items, multiple TaskSeq.mapAsync`` variant = task {
    let tskSeq = taskSeq {
        yield 1
        yield 2
    }

    // let's call MoveNext multiple times on an empty sequence
    let ts1 = tskSeq |> TaskSeq.mapAsync (fun i -> task { return i + 1 })
    let result1 = TaskSeq.toArray ts1
    let ts2 = ts1 |> TaskSeq.mapAsync (fun i -> task { return i + 1 })
    let result2 = TaskSeq.toArray ts2
    ()
}

[<Fact>]
let ``TaskSeq-toArray can be applied multiple times to the same sequence`` () =
    let tq = createDummyTaskSeq 10
    let (results1: _[]) = tq |> TaskSeq.toArray
    let (results2: _[]) = tq |> TaskSeq.toArray
    let (results3: _[]) = tq |> TaskSeq.toArray
    let (results4: _[]) = tq |> TaskSeq.toArray
    results1 |> should equal [| 1..10 |]
    results2 |> should equal [| 1..10 |]
    results3 |> should equal [| 1..10 |]
    results4 |> should equal [| 1..10 |]
