module TaskSeq.Test.Using

open System
open System.Threading.Tasks
open FSharp.Control
open FsUnit
open Xunit


type private OneGetter() =
    member _.Get1() = 1

type private Disposable(disposed: bool ref) =
    inherit OneGetter()

    interface IDisposable with
        member _.Dispose() = disposed.Value <- true

type private AsyncDisposable(disposed: bool ref) =
    inherit OneGetter()

    interface IAsyncDisposable with
        member _.DisposeAsync() = ValueTask(task { do disposed.Value <- true })

type private MultiDispose(disposed: int ref) =
    inherit OneGetter()

    interface IDisposable with
        member _.Dispose() = disposed.Value <- 1

    interface IAsyncDisposable with
        member _.DisposeAsync() = ValueTask(task { do disposed.Value <- -1 })

let private check = TaskSeq.length >> Task.map (should equal 1)

[<Fact>]
let ``CE taskSeq: Using when type implements IDisposable`` () =
    let disposed = ref false

    let ts = taskSeq {
        use x = new Disposable(disposed)
        yield x.Get1()
    }

    check ts
    |> Task.map (fun _ -> disposed.Value |> should be True)

[<Fact>]
let ``CE taskSeq: Using when type implements IAsyncDisposable`` () =
    let disposed = ref false

    let ts = taskSeq {
        use x = AsyncDisposable(disposed)
        yield x.Get1()
    }

    check ts
    |> Task.map (fun _ -> disposed.Value |> should be True)

[<Fact>]
let ``CE taskSeq: Using when type implements IDisposable and IAsyncDisposable`` () =
    let disposed = ref 0

    let ts = taskSeq {
        use x = new MultiDispose(disposed) // Used to fail to compile (see #97)
        yield x.Get1()
    }

    check ts
    |> Task.map (fun _ -> disposed.Value |> should equal -1) // should prefer IAsyncDisposable, which returns -1

[<Fact>]
let ``CE taskSeq: Using! when type implements IDisposable`` () =
    let disposed = ref false

    let ts = taskSeq {
        use! x = task { return new Disposable(disposed) }
        yield x.Get1()
    }

    check ts
    |> Task.map (fun _ -> disposed.Value |> should be True)

[<Fact>]
let ``CE taskSeq: Using! when type implements IAsyncDisposable`` () =
    let disposed = ref false

    let ts = taskSeq {
        use! x = task { return AsyncDisposable(disposed) }
        yield x.Get1()
    }

    check ts
    |> Task.map (fun _ -> disposed.Value |> should be True)

[<Fact>]
let ``CE taskSeq: Using! when type implements IDisposable and IAsyncDisposable`` () =
    let disposed = ref 0

    let ts = taskSeq {
        use! x = task { return new MultiDispose(disposed) } // Used to fail to compile (see #97)
        yield x.Get1()
    }

    check ts
    |> Task.map (fun _ -> disposed.Value |> should equal -1) // should prefer IAsyncDisposable, which returns -1
