namespace FSharpy.Tests

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy
open System.Collections.Generic


type Find(output) =

    //
    // TaskSeq.find
    // TaskSeq.findAsync
    // the tryXXX versions are at the bottom half
    //

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-find on an empty sequence raises KeyNotFoundException`` () =
        logStart output

        task {
            fun () -> TaskSeq.empty |> TaskSeq.find ((=) 12) |> Task.ignore
            |> should throwAsyncExact typeof<KeyNotFoundException>
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-find on an empty sequence raises KeyNotFoundException - variant`` () =
        logStart output

        task {
            fun () -> taskSeq { do () } |> TaskSeq.find ((=) 12) |> Task.ignore
            |> should throwAsyncExact typeof<KeyNotFoundException>
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-findAsync on an empty sequence raises KeyNotFoundException`` () =
        logStart output

        task {
            fun () ->
                TaskSeq.empty
                |> TaskSeq.findAsync (fun x -> task { return x = 12 })
                |> Task.ignore
            |> should throwAsyncExact typeof<KeyNotFoundException>
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-find sad path raises KeyNotFoundException`` () =
        logStart output

        task {
            fun () ->
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.find ((=) 0) // dummy tasks sequence starts at 1
                |> Task.ignore

            |> should throwAsyncExact typeof<KeyNotFoundException>
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-findAsync sad path raises KeyNotFoundException`` () =
        logStart output

        task {
            fun () ->
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.findAsync (fun x -> task { return x = 0 }) // dummy tasks sequence starts at 1
                |> Task.ignore

            |> should throwAsyncExact typeof<KeyNotFoundException>
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-find sad path raises KeyNotFoundException variant`` () =
        logStart output

        task {
            fun () ->
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.find ((=) 51) // dummy tasks sequence ends at 50
                |> Task.ignore

            |> should throwAsyncExact typeof<KeyNotFoundException>
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-findAsync sad path raises KeyNotFoundException variant`` () =
        logStart output

        task {
            fun () ->
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.findAsync (fun x -> task { return x = 51 }) // dummy tasks sequence ends at 50
                |> Task.ignore

            |> should throwAsyncExact typeof<KeyNotFoundException>
        }


    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-find happy path middle of seq`` () =
        logStart output

        task {
            let! twentyFive =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.find (fun x -> x < 26 && x > 24)

            twentyFive |> should equal 25
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-findAsync happy path middle of seq`` () =
        logStart output

        task {
            let! twentyFive =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.findAsync (fun x -> task { return x < 26 && x > 24 })

            twentyFive |> should equal 25
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-find happy path first item of seq`` () =
        logStart output

        task {
            let! first =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.find ((=) 1) // dummy tasks seq starts at 1

            first |> should equal 1
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-findAsync happy path first item of seq`` () =
        logStart output

        task {
            let! first =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.findAsync (fun x -> task { return x = 1 }) // dummy tasks seq starts at 1

            first |> should equal 1
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-find happy path last item of seq`` () =
        logStart output

        task {
            let! last =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.find ((=) 50) // dummy tasks seq ends at 50

            last |> should equal 50
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-findAsync happy path last item of seq`` () =
        logStart output

        task {
            let! last =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.findAsync (fun x -> task { return x = 50 }) // dummy tasks seq ends at 50

            last |> should equal 50
        }

    //
    // TaskSeq.tryFind
    // TaskSeq.tryFindAsync
    //

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFind on an empty sequence returns None`` () =
        logStart output

        task {
            let! nothing = TaskSeq.empty |> TaskSeq.tryFind ((=) 12)
            nothing |> should be None'
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFindAsync on an empty sequence returns None`` () =
        logStart output

        task {
            let! nothing =
                TaskSeq.empty
                |> TaskSeq.tryFindAsync (fun x -> task { return x = 12 })

            nothing |> should be None'
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFind sad path returns None`` () =
        logStart output

        task {
            let! nothing =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFind ((=) 0) // dummy tasks sequence starts at 1

            nothing |> should be None'
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFindAsync sad path return None`` () =
        logStart output

        task {
            let! nothing =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFindAsync (fun x -> task { return x = 0 }) // dummy tasks sequence starts at 1

            nothing |> should be None'
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFind sad path returns None variant`` () =
        logStart output

        task {
            let! nothing =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFind ((<=) 51) // dummy tasks sequence ends at 50 (inverted sign in lambda!)

            nothing |> should be None'
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFindAsync sad path return None - variant`` () =
        logStart output

        task {
            let! nothing =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFindAsync (fun x -> task { return x >= 51 }) // dummy tasks sequence ends at 50

            nothing |> should be None'
        }


    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFind happy path middle of seq`` () =
        logStart output

        task {
            let! twentyFive =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFind (fun x -> x < 26 && x > 24)

            twentyFive |> should be Some'
            twentyFive |> should equal (Some 25)
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFindAsync happy path middle of seq`` () =
        logStart output

        task {
            let! twentyFive =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFindAsync (fun x -> task { return x < 26 && x > 24 })

            twentyFive |> should be Some'
            twentyFive |> should equal (Some 25)
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFind happy path first item of seq`` () =
        logStart output

        task {
            let! first =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFind ((=) 1) // dummy tasks seq starts at 1

            first |> should be Some'
            first |> should equal (Some 1)
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFindAsync happy path first item of seq`` () =
        logStart output

        task {
            let! first =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFindAsync (fun x -> task { return x = 1 }) // dummy tasks seq starts at 1

            first |> should be Some'
            first |> should equal (Some 1)
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFind happy path last item of seq`` () =
        logStart output

        task {
            let! last =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFind ((=) 50) // dummy tasks seq ends at 50

            last |> should be Some'
            last |> should equal (Some 50)
        }

    [<Fact(Timeout = 10_000)>]
    let ``TaskSeq-tryFindAsync happy path last item of seq`` () =
        logStart output

        task {
            let! last =
                createDummyTaskSeqWith 50L<µs> 1000L<µs> 50
                |> TaskSeq.tryFindAsync (fun x -> task { return x = 50 }) // dummy tasks seq ends at 50

            last |> should be Some'
            last |> should equal (Some 50)
        }
