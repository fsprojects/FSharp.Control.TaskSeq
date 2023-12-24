module TaskSeq.Tests.Do

open System.Threading.Tasks

open FsUnit
open Xunit

open FSharp.Control

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
let ``CE taskSeq: use 'do!' with a valueTask<unit>`` () =
    let mutable value = 0

    taskSeq { do! ValueTask.ofTask (task { do value <- value + 1 }) }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'do!' with a non-generic valueTask`` () =
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
        do! ValueTask<_>(()) // unit valueTask that completes immediately
        do! Task.fromResult () // unit Task that completes immediately
        do! Task.Delay 0
        do! Async.Sleep 0
        do! async { value <- value + 1 } // eq 4
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 4)
