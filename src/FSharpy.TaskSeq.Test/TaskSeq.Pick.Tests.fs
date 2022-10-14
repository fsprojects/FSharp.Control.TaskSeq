module FSharpy.Tests.Pick

open System
open System.Collections.Generic
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


//
// TaskSeq.pick
// TaskSeq.pickAsync
// the tryXXX versions are at the bottom half
//

[<Fact>]
let ``TaskSeq-pick on an empty sequence raises KeyNotFoundException`` () = task {
    fun () ->
        TaskSeq.empty
        |> TaskSeq.pick (fun x -> if x = 12 then Some x else None)
        |> Task.ignore
    |> should throwAsyncExact typeof<KeyNotFoundException>
}

[<Fact>]
let ``TaskSeq-pick on an empty sequence raises KeyNotFoundException - variant`` () = task {
    fun () ->
        taskSeq { do () }
        |> TaskSeq.pick (fun x -> if x = 12 then Some x else None)
        |> Task.ignore
    |> should throwAsyncExact typeof<KeyNotFoundException>
}

[<Fact>]
let ``TaskSeq-pickAsync on an empty sequence raises KeyNotFoundException`` () = task {
    fun () ->
        TaskSeq.empty
        |> TaskSeq.pickAsync (fun x -> task { return if x = 12 then Some x else None })
        |> Task.ignore
    |> should throwAsyncExact typeof<KeyNotFoundException>
}

[<Fact>]
let ``TaskSeq-pick sad path raises KeyNotFoundException`` () = task {
    fun () ->
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pick (fun x -> if x = 0 then Some x else None) // dummy tasks sequence starts at 1
        |> Task.ignore

    |> should throwAsyncExact typeof<KeyNotFoundException>
}

[<Fact>]
let ``TaskSeq-pickAsync sad path raises KeyNotFoundException`` () = task {
    fun () ->
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pickAsync (fun x -> task { return if x < 0 then Some x else None }) // dummy tasks sequence starts at 1
        |> Task.ignore

    |> should throwAsyncExact typeof<KeyNotFoundException>
}

[<Fact>]
let ``TaskSeq-pick sad path raises KeyNotFoundException variant`` () = task {
    fun () ->
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pick (fun x -> if x = 51 then Some x else None) // dummy tasks sequence ends at 50
        |> Task.ignore

    |> should throwAsyncExact typeof<KeyNotFoundException>
}

[<Fact>]
let ``TaskSeq-pickAsync sad path raises KeyNotFoundException variant`` () = task {
    fun () ->
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pickAsync (fun x -> task { return if x = 51 then Some x else None }) // dummy tasks sequence ends at 50
        |> Task.ignore

    |> should throwAsyncExact typeof<KeyNotFoundException>
}


[<Fact>]
let ``TaskSeq-pick happy path middle of seq`` () = task {
    let! twentyFive =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pick (fun x -> if x < 26 && x > 24 then Some "foo" else None)

    twentyFive |> should equal "foo"
}

[<Fact>]
let ``TaskSeq-pickAsync happy path middle of seq`` () = task {
    let! twentyFive =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pickAsync (fun x -> task { return if x < 26 && x > 24 then Some "foo" else None })

    twentyFive |> should equal "foo"
}

[<Fact>]
let ``TaskSeq-pick happy path first item of seq`` () = task {
    let! first =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pick (fun x -> if x = 1 then Some $"first{x}" else None) // dummy tasks seq starts at 1

    first |> should equal "first1"
}

[<Fact>]
let ``TaskSeq-pickAsync happy path first item of seq`` () = task {
    let! first =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pickAsync (fun x -> task { return if x = 1 then Some $"first{x}" else None }) // dummy tasks seq starts at 1

    first |> should equal "first1"
}

[<Fact>]
let ``TaskSeq-pick happy path last item of seq`` () = task {
    let! last =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pick (fun x -> if x = 50 then Some $"last{x}" else None) // dummy tasks seq ends at 50

    last |> should equal "last50"
}

[<Fact>]
let ``TaskSeq-pickAsync happy path last item of seq`` () = task {
    let! last =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.pickAsync (fun x -> task { return if x = 50 then Some $"last{x}" else None }) // dummy tasks seq ends at 50

    last |> should equal "last50"
}

//
// TaskSeq.tryPick
// TaskSeq.tryPickAsync
//

[<Fact>]
let ``TaskSeq-tryPick on an empty sequence returns None`` () = task {
    let! nothing =
        TaskSeq.empty
        |> TaskSeq.tryPick (fun x -> if x = 12 then Some x else None)

    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryPickAsync on an empty sequence returns None`` () = task {
    let! nothing =
        TaskSeq.empty
        |> TaskSeq.tryPickAsync (fun x -> task { return if x = 12 then Some x else None })

    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryPick sad path returns None`` () = task {
    let! nothing =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPick (fun x -> if x = 0 then Some x else None) // dummy tasks sequence starts at 1

    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryPickAsync sad path return None`` () = task {
    let! nothing =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPickAsync (fun x -> task { return if x = 0 then Some x else None }) // dummy tasks sequence starts at 1

    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryPick sad path returns None variant`` () = task {
    let! nothing =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPick (fun x -> if x >= 51 then Some x else None) // dummy tasks sequence ends at 50 (inverted sign in lambda!)

    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryPickAsync sad path return None - variant`` () = task {
    let! nothing =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPickAsync (fun x -> task { return if x >= 51 then Some x else None }) // dummy tasks sequence ends at 50

    nothing |> should be None'
}


[<Fact>]
let ``TaskSeq-tryPick happy path middle of seq`` () = task {
    let! twentyFive =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPick (fun x -> if x < 26 && x > 24 then Some $"foo{x}" else None)

    twentyFive |> should be Some'
    twentyFive |> should equal (Some "foo25")
}

[<Fact>]
let ``TaskSeq-tryPickAsync happy path middle of seq`` () = task {
    let! twentyFive =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPickAsync (fun x -> task { return if x < 26 && x > 24 then Some $"foo{x}" else None })

    twentyFive |> should be Some'
    twentyFive |> should equal (Some "foo25")
}

[<Fact>]
let ``TaskSeq-tryPick happy path first item of seq`` () = task {
    let! first =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPick (sprintf "foo%i" >> Some) // dummy tasks seq starts at 1

    first |> should be Some'
    first |> should equal (Some "foo1")
}

[<Fact>]
let ``TaskSeq-tryPickAsync happy path first item of seq`` () = task {
    let! first =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPickAsync (fun x -> task { return (sprintf "foo%i" >> Some) x }) // dummy tasks seq starts at 1

    first |> should be Some'
    first |> should equal (Some "foo1")
}

[<Fact>]
let ``TaskSeq-tryPick happy path last item of seq`` () = task {
    let! last =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPick (fun x -> if x = 50 then Some $"foo{x}" else None) // dummy tasks seq ends at 50

    last |> should be Some'
    last |> should equal (Some "foo50")
}

[<Fact>]
let ``TaskSeq-tryPickAsync happy path last item of seq`` () = task {
    let! last =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryPickAsync (fun x -> task { return if x = 50 then Some $"foo{x}" else None }) // dummy tasks seq ends at 50

    last |> should be Some'
    last |> should equal (Some "foo50")
}
