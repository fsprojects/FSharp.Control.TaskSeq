module TaskSeq.Tests.Do

open System
open System.Threading.Tasks
open FsUnit
open Xunit

open FSharp.Control

[<Fact>]
let ``CE taskSeq: use 'do'`` () =
    let mutable value = 0

    taskSeq { do value <- value + 1 }

    |> verifyEmpty

[<Fact>]
let ``CE taskSeq: use 'do!' with a task<unit>`` () =
    let mutable value = 0

    taskSeq { do! task { do value <- value + 1 } }

    |> verifyEmpty

//[<Fact>]
//let ``CE taskSeq: use 'do!' with a valuetask<unit>`` () =
//    let mutable value = 0

//    taskSeq { do! ValueTask.ofIValueTaskSource (task { do value <- value + 1 }) }

//    |> verifyEmpty

//[<Fact>]
//let ``CE taskSeq: use 'do!' with a non-generic valuetask`` () =
//    let mutable value = 0

//    taskSeq { do! ValueTask(task { do value <- value + 1 }) }

//    |> verifyEmpty

//[<Fact>]
//let ``CE taskSeq: use 'do!' with a non-generic task`` () =
//    let mutable value = 0

//    taskSeq { do! (task { do value <- value + 1 }) |> Task.ignore }

//    |> verifyEmpty
