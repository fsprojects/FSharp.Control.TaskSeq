namespace FSharpy.Tests

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

type Head(output) =

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-head throws on empty sequences`` () =
        logStart output

        task {
            fun () -> TaskSeq.empty<string> |> TaskSeq.head |> Task.ignore
            |> should throwAsyncExact typeof<ArgumentException>
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-head throws on empty sequences - variant`` () =
        logStart output

        task {
            fun () -> taskSeq { do () } |> TaskSeq.head |> Task.ignore
            |> should throwAsyncExact typeof<ArgumentException>
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryHead returns None on empty sequences`` () =
        logStart output

        task {
            let! nothing = TaskSeq.empty<string> |> TaskSeq.tryHead
            nothing |> should be None'
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-head gets the first item in a longer sequence`` () =
        logStart output

        task {
            let! head = createDummyTaskSeqWith 50L<µs> 1000L<µs> 50 |> TaskSeq.head

            head |> should equal 1
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-head gets the only item in a singleton sequence`` () =
        logStart output

        task {
            let! head = taskSeq { yield 10 } |> TaskSeq.head
            head |> should equal 10
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryHead gets the first item in a longer sequence`` () =
        logStart output

        task {
            let! head =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryHead

            head |> should be Some'
            head |> should equal (Some 1)
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryHead gets the only item in a singleton sequence`` () =
        logStart output

        task {
            let! head = taskSeq { yield 10 } |> TaskSeq.tryHead
            head |> should be Some'
            head |> should equal (Some 10)
        }
