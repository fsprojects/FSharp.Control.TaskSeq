module FSharpy.Tests.Head

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Fact(Timeout = 10_000)>]
let ``TaskSeq-head throws on empty sequences`` () = task {
    fun () -> TaskSeq.empty<string> |> TaskSeq.head |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-head throws on empty sequences - variant`` () = task {
    fun () -> taskSeq { do () } |> TaskSeq.head |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-tryHead returns None on empty sequences`` () = task {
    let! nothing = TaskSeq.empty<string> |> TaskSeq.tryHead
    nothing |> should be None'
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-head gets the first item in a longer sequence`` () = task {
    let! head = createDummyTaskSeqWith 50L<µs> 1000L<µs> 50 |> TaskSeq.head

    head |> should equal 1
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-head gets the only item in a singleton sequence`` () = task {
    let! head = taskSeq { yield 10 } |> TaskSeq.head
    head |> should equal 10
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-tryHead gets the first item in a longer sequence`` () = task {
    let! head =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryHead

    head |> should be Some'
    head |> should equal (Some 1)
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-tryHead gets the only item in a singleton sequence`` () = task {
    let! head = taskSeq { yield 10 } |> TaskSeq.tryHead
    head |> should be Some'
    head |> should equal (Some 10)
}
