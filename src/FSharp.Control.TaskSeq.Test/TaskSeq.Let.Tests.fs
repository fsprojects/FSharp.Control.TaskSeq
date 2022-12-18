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

[<Fact>]
let ``CE taskSeq: use 'let!' with Async`` () =
    let mutable value = 0

    taskSeq {
        do value <- value + 1
        let! _ = Async.Sleep 50
        do value <- value + 1
    }
    |> verifyEmpty
    |> Task.map (fun _ -> value |> should equal 2)

[<Fact>]
let ``CE taskSeq: use 'let!' with Async - mutables`` () =
    let mutable value = 0

    taskSeq {
        do! async { value <- value + 1 }
        let! x = async { return value + 1 }
        do! Async.Sleep 50
        do! async { value <- value + 1 }
        let! ret = async { return value + 1 }
        yield x + ret // eq 6
    }
    |> TaskSeq.exactlyOne
    |> Task.map (fun _ -> value |> should equal 6)

[<Fact>]
let ``CE taskSeq: use 'let!' with all kinds of overloads at once`` () =
    let mutable value = 0

    // this test should be expanded in case any new overload is added
    // that is supported by `let!`, to ensure the high/low priority
    // overloads still work properly
    taskSeq {
        let! a = task { // eq 1
            do! Task.Delay 10
            do value <- value + 1
            return value
        }

        let! b =  // eq 2
            task {
                do! Task.Delay 50
                do value <- value + 1
                return value
            }
            |> ValueTask<int>

        let! c = ValueTask<_>(4) // valuetask that completes immediately
        let! _ = Task.Factory.StartNew(fun () -> value <- value + 1) // non-generic Task with side effect
        let! d = Task.fromResult (4) // normal Task that completes immediately
        let! _ = Async.Sleep 0 // unit Async

        let! e = async {
            do! Async.Sleep 40
            do value <- value + 1
            return value
        }

        yield! [ a; b; c; d; e ]
    }
    |> TaskSeq.toListAsync
    |> Task.map (fun x -> should equal [ 1; 2; 4; 4; 3 ])
