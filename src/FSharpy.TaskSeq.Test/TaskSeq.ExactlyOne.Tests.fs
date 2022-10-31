module FSharpy.Tests.ExactlyOne

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Fact>]
let ``TaskSeq-exactlyOne throws on empty sequences`` () = task {
    fun () -> TaskSeq.empty<string> |> TaskSeq.exactlyOne |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact>]
let ``TaskSeq-exactlyOne throws on empty sequences - variant`` () = task {
    fun () -> taskSeq { do () } |> TaskSeq.exactlyOne |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact>]
let ``TaskSeq-tryExactlyOne returns None on empty sequences`` () = task {
    let! nothing = TaskSeq.empty<string> |> TaskSeq.tryExactlyOne
    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-exactlyOne throws for a sequence of length = two`` () = task {
    fun () ->
        taskSeq {
            yield 1
            yield 2
        }
        |> TaskSeq.exactlyOne
        |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact>]
let ``TaskSeq-exactlyOne throws for a sequence of length = two - variant`` () = task {
    fun () ->
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 2
        |> TaskSeq.exactlyOne
        |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}


[<Fact>]
let ``TaskSeq-exactlyOne throws with a larger sequence`` () = task {
    fun () ->
        Gen.sideEffectTaskSeqMicro 50L<µs> 300L<µs> 200
        |> TaskSeq.exactlyOne
        |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact>]
let ``TaskSeq-tryExactlyOne returns None with a larger sequence`` () = task {
    let! nothing =
        Gen.sideEffectTaskSeqMicro 50L<µs> 300L<µs> 20
        |> TaskSeq.tryExactlyOne

    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-exactlyOne gets the only item in a singleton sequence`` () = task {
    let! exactlyOne = taskSeq { yield 10 } |> TaskSeq.exactlyOne
    exactlyOne |> should equal 10
}

[<Fact>]
let ``TaskSeq-tryExactlyOne gets the only item in a singleton sequence`` () = task {
    let! exactlyOne = taskSeq { yield 10 } |> TaskSeq.tryExactlyOne
    exactlyOne |> should be Some'
    exactlyOne |> should equal (Some 10)
}

[<Fact>]
let ``TaskSeq-exactlyOne gets the only item in a singleton sequence - variant`` () = task {
    let! exactlyOne =
        Gen.sideEffectTaskSeqMs 50<ms> 300<ms> 1
        |> TaskSeq.exactlyOne

    exactlyOne |> should equal 1
}

[<Fact>]
let ``TaskSeq-tryExactlyOne gets the only item in a singleton sequence - variant`` () = task {
    let! exactlyOne =
        Gen.sideEffectTaskSeqMs 50<ms> 300<ms> 1
        |> TaskSeq.tryExactlyOne

    exactlyOne |> should be Some'
    exactlyOne |> should equal (Some 1)
}
