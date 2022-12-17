module TaskSeq.Tests.Do

open System.Threading.Tasks

open FsUnit
open Xunit

open FSharp.Control
open System.Threading

[<Fact>]
let ``CE taskSeq: use 'do'`` () =
    let mutable value = 0

    taskSeq { do value <- value + 1 } |> verifyEmpty

[<Fact>]
let ``CE taskSeq: use 'do!' with a task<unit>`` () =
    let mutable value = 0

    taskSeq { do! task { do value <- value + 1 } }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'do!' with a ValueTask<unit>`` () =
    let mutable value = 0

    taskSeq { do! ValueTask.ofTask (task { do value <- value + 1 }) }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'do!' with a non-generic ValueTask`` () =
    let mutable value = 0

    taskSeq { do! ValueTask(task { do value <- value + 1 }) }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'do!' with a non-generic task`` () =
    let mutable value = 0

    taskSeq { do! task { do value <- value + 1 } |> Task.ignore }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'do!' with a task-delay`` () =
    let mutable value = 0

    taskSeq {
        do value <- value + 1
        do! Task.Delay 50
        do value <- value + 1
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 2)

//module CancellationToken =
//    [<Fact>]
//    let ``CE taskSeq: use 'do!' with a default cancellation-token`` () =
//        let mutable value = 0

//        taskSeq {
//            do value <- value + 1
//            do! CancellationToken()
//            do value <- value + 1
//        }
//        |> verifyEmpty
//        |> Task.map (fun _ -> value |> should equal 2)

//    [<Fact>]
//    let ``CE taskSeq: use 'do!' with a timer cancellation-token - explicit`` () = task {
//        let mutable value = 0
//        use tokenSource = new CancellationTokenSource(500)

//        return!
//            taskSeq {
//                do! tokenSource.Token // this sets the token for this taskSeq
//                do value <- value + 1
//                do! Task.Delay(300, tokenSource.Token)
//                do! Task.Delay(300, tokenSource.Token)
//                do! Task.Delay(300, tokenSource.Token)
//                do value <- value + 1
//            }
//            |> verifyEmpty
//            |> Task.map (fun _ -> value |> should equal 2)
//    }


//    [<Fact>]
//    let ``CE taskSeq: use 'do!' with a timer cancellation-token - implicit`` () = task {
//        let mutable value = 0
//        use tokenSource = new CancellationTokenSource(500)

//        return!
//            taskSeq {
//                do! tokenSource.Token // this sets the token for this taskSeq
//                do value <- value + 1
//                do! Task.Delay(300)
//                do! Task.Delay(300)
//                do! Task.Delay(300)
//                do value <- value + 1
//            }
//            |> verifyEmpty
//            |> Task.map (fun _ -> value |> should equal 2)
//    }

[<Fact>]
let ``CE taskSeq: use 'do!' with Async`` () =
    let mutable value = 0

    taskSeq {
        do value <- value + 1
        do! Async.Sleep 50
        do value <- value + 1
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 2)

[<Fact>]
let ``CE taskSeq: use 'do!' with Async - mutables`` () =
    let mutable value = 0

    taskSeq {
        do! async { value <- value + 1 }
        do! Async.Sleep 50
        do! async { value <- value + 1 }
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 2)

[<Fact>]
let ``CE taskSeq: use 'do!' with all kinds of overloads at once`` () =
    let mutable value = 0

    // this test should be expanded in case any new overload is added
    // that is supported by `do!`, to ensure the high/low priority
    // overloads still work properly
    taskSeq {
        do! task { do value <- value + 1 } |> Task.ignore
        do! ValueTask <| task { do value <- value + 1 }
        do! ValueTask.ofTask (task { do value <- value + 1 })
        do! ValueTask<_>(()) // unit ValueTask that completes immediately
        do! Task.fromResult (()) // unit Task that completes immediately
        do! Task.Delay 0
        do! Async.Sleep 0
        do! async { value <- value + 1 } // eq 4
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 4)
