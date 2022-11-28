module Tests

open System
open System.Threading.Tasks
open Xunit
open FSharp.Control

[<Fact>]
let ``Use and execute a taskSeq from nuget`` () =
    taskSeq { yield 10 }
    |> TaskSeq.toArray
    |> fun x -> Assert.Equal<int array>(x, [| 10 |])

[<Fact>]
let ``Use and execute a taskSeq from nuget with multiple keywords`` () =
    taskSeq {
        do! task { do! Task.Delay 10 }
        let! x = task { return 1 }
        yield x
        let! vt = ValueTask<int>(task { return 2 })
        yield vt
        yield 10
    }
    |> TaskSeq.toArray
    |> fun x -> Assert.Equal<int array>(x, [| 1; 2; 10 |])

// from version 0.3.0:

//[<Fact>]
//let ``Use and execute a taskSeq from nuget with multiple keywords v0.3.0`` () =
//    taskSeq {
//        do! task { do! Task.Delay 10 }
//        do! Task.Delay 10 // only in 0.3
//        let! x = task { return 1 } :> Task // only in 0.3
//        yield 1
//        let! vt = ValueTask<int> ( task { return 2 } )
//        yield vt
//        let! vt = ValueTask ( task { return 2 } ) // only in 0.3
//        do! ValueTask ( task { return 2 } ) // only in 0.3
//        yield 3
//        yield 10
//    }
//    |> TaskSeq.toArray
//    |> fun x -> Assert.Equal<int array>(x, [| 1; 2; 3; 10 |])
