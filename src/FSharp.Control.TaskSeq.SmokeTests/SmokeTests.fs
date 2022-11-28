module Tests

open System
open System.Threading.Tasks
open Xunit
open FSharp.Control
open FsUnit.Xunit

//
// this file can be used to hand-test NuGet deploys
// esp. when there are changes in the surface area
//
// This project gets compiled in CI, but is not part
// of the structured test reports currently.
// However, a compile error will fail the CI pipeline.
//


type private MultiDispose(disposed: int ref) =
    member _.Get1() = 1

    interface IDisposable with
        member _.Dispose() = disposed.Value <- 1

    interface IAsyncDisposable with
        member _.DisposeAsync() = ValueTask(task { do disposed.Value <- -1 })

[<Fact>]
let ``Use and execute a minimal taskSeq from nuget`` () =
    taskSeq { yield 10 }
    |> TaskSeq.toArray
    |> fun x -> Assert.Equal<int array>(x, [| 10 |])

[<Fact>]
let ``Use taskSeq from nuget with multiple keywords v0.2.2`` () =
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

[<Fact>]
let ``Use taskSeq from nuget with multiple keywords v0.3.0`` () =
    taskSeq {
        do! task { do! Task.Delay 10 }
        do! Task.Delay 10 // only in 0.3
        let! x = task { return 1 } :> Task // only in 0.3
        yield 1
        let! vt = ValueTask<int>(task { return 2 })
        yield vt
        let! vt = ValueTask(task { return 2 }) // only in 0.3
        do! ValueTask(task { return 2 }) // only in 0.3
        yield 3
        yield 10
    }
    |> TaskSeq.toArray
    |> fun x -> Assert.Equal<int array>(x, [| 1; 2; 3; 10 |])

[<Fact>]
let ``Use taskSeq when type implements IDisposable and IAsyncDisposable`` () =
    let disposed = ref 0

    let ts = taskSeq {
        use! x = task { return new MultiDispose(disposed) } // Used to fail to compile (see #97, fixed in v0.3.0)
        yield x.Get1()
    }

    ts
    |> TaskSeq.length
    |> Task.map (should equal 1)
    |> Task.map (fun _ -> disposed.Value |> should equal -1) // must favor IAsyncDisposable, not IDisposable

[<Fact>]
let ``Use taskSeq as part of an F# task CE`` () = task {
    let ts = taskSeq { yield! [ 0..99 ] }
    let ra = ResizeArray()

    // loop through a taskSeq, support added in v0.3.0
    for v in ts do
        ra.Add v

    ra.ToArray() |> should equal [| 0..99 |]
}

[<Fact>]
let ``New surface area functions availability tests v0.3.0`` () = task {
    let ts = TaskSeq.singleton 10 // added in v0.3.0
    let! ls = TaskSeq.toListAsync ts
    List.exactlyOne ls |> should equal 10
}
