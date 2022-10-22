module FSharpy.Tests.``State transition bug and InvalidState``

open System
open System.Threading.Tasks
open System.Diagnostics
open System.Collections.Generic

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

let getEmptyVariant variant : IAsyncEnumerable<int> =
    match variant with
    | "do" -> taskSeq { do ignore () }
    | "do!" -> taskSeq { do! task { return () } } // TODO: this doesn't work with Task, only Task<unit>...
    | "yield! (seq)" -> taskSeq { yield! Seq.empty<int> }
    | "yield! (taskseq)" -> taskSeq { yield! taskSeq { do ignore () } }
    | _ -> failwith "Uncovered variant of test"

/// Call MoveNextAsync() and check if return value is the expected value
let moveNextAndCheck expected (enumerator: IAsyncEnumerator<_>) = task {
    let! (hasNext: bool) = enumerator.MoveNextAsync()

    if expected then
        hasNext |> should be True
    else
        hasNext |> should be False
}

[<Fact>]
let ``CE empty taskSeq with MoveNextAsync -- untyped`` () = task {
    let tskSeq = taskSeq { do ignore () }

    Assert.IsAssignableFrom<IAsyncEnumerable<obj>>(tskSeq)
    |> ignore

    do! moveNextAndCheck false (tskSeq.GetAsyncEnumerator())
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE empty taskSeq with MoveNextAsync -- typed`` variant = task {
    let tskSeq = getEmptyVariant variant

    Assert.IsAssignableFrom<IAsyncEnumerable<int>>(tskSeq)
    |> ignore

    do! moveNextAndCheck false (tskSeq.GetAsyncEnumerator())
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE  empty taskSeq, GetAsyncEnumerator multiple times`` variant = task {
    let tskSeq = getEmptyVariant variant
    use _e = tskSeq.GetAsyncEnumerator()
    use _e = tskSeq.GetAsyncEnumerator()
    use _e = tskSeq.GetAsyncEnumerator()
    ()
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE  empty taskSeq, GetAsyncEnumerator multiple times and then MoveNextAsync`` variant = task {
    let tskSeq = getEmptyVariant variant
    use enumerator = tskSeq.GetAsyncEnumerator()
    use enumerator = tskSeq.GetAsyncEnumerator()
    do! moveNextAndCheck false enumerator
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE empty taskSeq, GetAsyncEnumerator + MoveNextAsync multiple times`` variant = task {
    let tskSeq = getEmptyVariant variant
    use enumerator1 = tskSeq.GetAsyncEnumerator()
    do! moveNextAndCheck false enumerator1

    // getting the enumerator again
    use enumerator2 = tskSeq.GetAsyncEnumerator()
    do! moveNextAndCheck false enumerator1 // original should still work without raising
    do! moveNextAndCheck false enumerator2 // new hone should also work without raising
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE empty taskSeq, GetAsyncEnumerator + MoveNextAsync in a loop`` variant = task {
    let tskSeq = getEmptyVariant variant

    // let's get the enumerator a few times
    for i in 0..100 do
        use enumerator = tskSeq.GetAsyncEnumerator()
        do! moveNextAndCheck false enumerator // these are all empty
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE empty taskSeq, call Current before MoveNextAsync`` variant = task {
    let tskSeq = getEmptyVariant variant
    let enumerator = tskSeq.GetAsyncEnumerator()

    // call Current *before* MoveNextAsync
    let current = enumerator.Current
    current |> should equal 0 // we return Unchecked.defaultof, which is Zero in the case of an integer
}

[<Theory; InlineData "do"; InlineData "do!"; InlineData "yield! (seq)"; InlineData "yield! (taskseq)">]
let ``CE empty taskSeq, call Current after MoveNextAsync returns false`` variant = task {
    let tskSeq = getEmptyVariant variant
    let enumerator = tskSeq.GetAsyncEnumerator()
    do! moveNextAndCheck false enumerator // false for empty seq

    // call Current *after* MoveNextAsync returns false
    enumerator.Current |> should equal 0 // we return Unchecked.defaultof, which is Zero in the case of an integer
}

[<Fact>]
let ``CE taskSeq with two items, call Current before MoveNextAsync`` () = task {
    let tskSeq = taskSeq {
        yield "foo"
        yield "bar"
    }

    let enumerator = tskSeq.GetAsyncEnumerator()

    // call Current before MoveNextAsync
    let current = enumerator.Current
    current |> should be Null // we return Unchecked.defaultof
}

[<Fact>]
let ``CE taskSeq with two items, call Current after MoveNextAsync returns false`` () = task {
    let tskSeq = taskSeq {
        yield "foo"
        yield "bar"
    }

    let enum = tskSeq.GetAsyncEnumerator()
    do! moveNextAndCheck true enum // first item
    do! moveNextAndCheck true enum // second item
    do! moveNextAndCheck false enum // third item: false

    // call Current *after* MoveNextAsync returns false
    enum.Current |> should be Null // we return Unchecked.defaultof
}

[<Fact>]
let ``CE taskSeq with two items, MoveNext once too far`` () = task {
    let tskSeq = taskSeq {
        yield 1
        yield 2
    }

    let enum = tskSeq.GetAsyncEnumerator()
    do! moveNextAndCheck true enum // first item
    do! moveNextAndCheck true enum // second item
    do! moveNextAndCheck false enum // third item: false
    do! moveNextAndCheck false enum // this used to be an error, see issue #39 and PR #42
}

[<Fact>]
let ``CE taskSeq with two items, MoveNext too far`` () = task {
    let tskSeq = taskSeq {
        yield Guid.NewGuid()
        yield Guid.NewGuid()
    }

    // let's call MoveNext multiple times on an empty sequence
    let enum = tskSeq.GetAsyncEnumerator()

    // first get past the post
    do! moveNextAndCheck true enum // first item
    do! moveNextAndCheck true enum // second item
    do! moveNextAndCheck false enum // third item: false

    // then call it bunch of times to ensure we don't get an InvalidOperationException, see issue #39 and PR #42
    for i in 0..100 do
        do! moveNextAndCheck false enum

    // after whatever amount of time MoveNextAsync, we can still safely call Current
    enum.Current |> should equal Guid.Empty // we return Unchecked.defaultof, which is Guid.Empty for guids
}

[<Fact>]
let ``CE taskSeq with two items, multiple TaskSeq.map`` () = task {
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

[<Fact>]
let ``CE taskSeq with two items, multiple TaskSeq.mapAsync`` () = task {
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
