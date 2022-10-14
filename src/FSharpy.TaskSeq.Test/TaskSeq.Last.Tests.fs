module FSharpy.Tests.Last

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Fact(Timeout = 10_000)>]
let ``TaskSeq-last throws on empty sequences`` () = task {
    fun () -> TaskSeq.empty<string> |> TaskSeq.last |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-last throws on empty sequences - variant`` () = task {
    fun () -> taskSeq { do () } |> TaskSeq.last |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-tryLast returns None on empty sequences`` () = task {
    let! nothing = TaskSeq.empty<string> |> TaskSeq.tryLast
    nothing |> should be None'
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-last gets the last item in a longer sequence`` () = task {
    let! last = createDummyTaskSeqWith 50L<µs> 1000L<µs> 50 |> TaskSeq.last

    last |> should equal 50
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-last gets the only item in a singleton sequence`` () = task {
    let! last = taskSeq { yield 10 } |> TaskSeq.last
    last |> should equal 10
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-tryLast gets the last item in a longer sequence`` () = task {
    let! last =
        createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryLast

    last |> should be Some'
    last |> should equal (Some 50)
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-tryLast gets the only item in a singleton sequence`` () = task {
    let! last = taskSeq { yield 10 } |> TaskSeq.tryLast
    last |> should be Some'
    last |> should equal (Some 10)
}
