module FSharp.Control.TaskSeq.Test

open System
open System.Threading.Tasks
open FSharp.Control
open FsUnit
open Xunit


type private OneGetter() =
    member _.Get1() = 1

type private Disposable() =
    inherit OneGetter()

    interface IDisposable with
        member _.Dispose() = ()

type private AsyncDisposable() =
    inherit OneGetter()

    interface IAsyncDisposable with
        member _.DisposeAsync() = ValueTask()

type private MultiDispose() =
    inherit OneGetter()

    interface IDisposable with
        member _.Dispose() =
            ()

    interface IAsyncDisposable with
        member _.DisposeAsync() =
            ValueTask()

let private check ts = task {
    let! length = ts |> TaskSeq.length
    length |> should equal 1
}

[<Fact>]
let ``CE task: Using when type implements IDisposable``() =
    let ts = taskSeq {
        use x = new Disposable()

        yield x.Get1()
    }

    check ts

[<Fact>]
let ``CE task: Using when type implements IAsyncDisposable``() =
    let ts = taskSeq {
        use x = AsyncDisposable()
        yield x.Get1()
    }

    check ts


[<Fact>]
let ``CE task: Using when type implements IDisposable and IAsyncDisposable``() =
    let ts = taskSeq {
        use x = new MultiDispose()
        yield x.Get1()
    }

    check ts

[<Fact>]
let ``CE task: Using! when type implements IDisposable``() =
    let ts = taskSeq {
        use! x = task { return new Disposable() }
        yield x.Get1()
    }

    check ts


[<Fact>]
let ``CE task: Using! when type implements IAsyncDisposable``() =
    let ts = taskSeq {
        use! x = task { return AsyncDisposable() }
        yield x.Get1()
    }

    check ts


[<Fact>]
let ``CE task: Using! when type implements IDisposable and IAsyncDisposable``() =
    let ts = taskSeq {
        use! x = task { return new MultiDispose() }
        yield x.Get1()
    }

    check ts
