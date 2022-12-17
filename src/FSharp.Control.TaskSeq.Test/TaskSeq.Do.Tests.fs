module TaskSeq.Tests.Do

open System
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
let ``CE taskSeq: use 'do!' with a valuetask<unit>`` () =
    let mutable value = 0

    taskSeq { do! ValueTask.ofTask (task { do value <- value + 1 }) }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'do!' with a non-generic valuetask`` () =
    let mutable value = 0

    taskSeq { do! ValueTask(task { do value <- value + 1 }) }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'do!' with a non-generic task`` () =
    let mutable value = 0

    taskSeq { do! (task { do value <- value + 1 }) |> Task.ignore }
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
