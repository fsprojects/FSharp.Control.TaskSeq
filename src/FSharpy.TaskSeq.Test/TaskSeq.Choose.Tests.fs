namespace FSharpy.Tests

open System
open System.Threading.Tasks

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy
open Xunit.Abstractions


type Choose(output: ITestOutputHelper) =

    [<Fact(Timeout = 10_000)>]
    let ``ZHang timeout test`` () =
        logStart output

        task {
            let! empty = Task.Delay 30
            empty |> should be Null
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-choose on an empty sequence`` () =
        logStart output

        task {
            let! empty =
                TaskSeq.empty
                |> TaskSeq.choose (fun _ -> Some 42)
                |> TaskSeq.toListAsync

            List.isEmpty empty |> should be True
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-chooseAsync on an empty sequence`` () =
        logStart output

        task {
            let! empty =
                TaskSeq.empty
                |> TaskSeq.chooseAsync (fun _ -> task { return Some 42 })
                |> TaskSeq.toListAsync

            List.isEmpty empty |> should be True
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-choose can convert and filter`` () =
        logStart output

        task {
            let! alphabet =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.choose (fun number -> if number <= 26 then Some(char number + '@') else None)
                |> TaskSeq.toArrayAsync

            String alphabet |> should equal "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-chooseAsync can convert and filter`` () =
        logStart output

        task {
            let! alphabet =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.choose (fun number -> if number <= 26 then Some(char number + '@') else None)
                |> TaskSeq.toArrayAsync

            String alphabet |> should equal "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        }
