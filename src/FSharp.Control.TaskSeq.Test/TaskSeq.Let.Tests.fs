module TaskSeq.Tests.Let

open System
open System.Threading.Tasks
open FsUnit
open Xunit

open FSharp.Control

[<Fact>]
let ``CE taskSeq: use 'let'`` () =
    let mutable value = 0

    taskSeq {
        let value1 = value + 1
        let value2 = value1 + 1
        yield value2
    }
    |> TaskSeq.exactlyOne
    |> Task.map (should equal 2)

[<Fact>]
let ``CE taskSeq: use 'let!' with a task<unit>`` () =
    let mutable value = 0

    taskSeq {
        let! unit' = task { do value <- value + 1 }
        do unit'
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'let!' with a task<string>`` () =
    taskSeq {
        let! test = task { return "test" }
        yield test
    }
    |> TaskSeq.exactlyOne
    |> Task.map (should equal "test")

[<Fact>]
let ``CE taskSeq: use 'let!' with a valuetask<unit>`` () =
    let mutable value = 0

    taskSeq {
        let! unit' = ValueTask.ofTask (task { do value <- value + 1 })
        do unit'
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'let!' with a valuetask<string>`` () =
    taskSeq {
        let! test = ValueTask.ofTask (task { return "test" })
        yield test
    }
    |> TaskSeq.exactlyOne
    |> Task.map (should equal "test")

[<Fact>]
let ``CE taskSeq: use 'let!' with a non-generic valuetask`` () =
    let mutable value = 0

    taskSeq {
        let! unit' = ValueTask(task { do value <- value + 1 })
        do unit'
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)

[<Fact>]
let ``CE taskSeq: use 'let!' with a non-generic task`` () =
    let mutable value = 0

    taskSeq {
        let! unit' = (task { do value <- value + 1 }) |> Task.ignore
        do unit'
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 1)
