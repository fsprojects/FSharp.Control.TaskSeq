module TaskSeq.Tests.Using

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

/// Tracks how many times Dispose/DisposeAsync has been called.
type private CountingDisposable(disposeCount: int ref) =
    interface IDisposable with
        member _.Dispose() = disposeCount.Value <- disposeCount.Value + 1

/// Tracks how many times DisposeAsync has been called.
type private CountingAsyncDisposable(disposeCount: int ref) =
    interface IAsyncDisposable with
        member _.DisposeAsync() =
            disposeCount.Value <- disposeCount.Value + 1
            ValueTask.CompletedTask

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

module SideEffects =
    [<Fact>]
    let ``CE taskSeq: use - Dispose called exactly once per full iteration`` () = task {
        let disposeCount = ref 0

        let ts = taskSeq {
            use _ = new CountingDisposable(disposeCount)
            yield 1
        }

        do! ts |> TaskSeq.iter ignore
        disposeCount.Value |> should equal 1
    }

    [<Fact>]
    let ``CE taskSeq: use - Dispose called on each re-iteration`` () = task {
        let disposeCount = ref 0

        let ts = taskSeq {
            use _ = new CountingDisposable(disposeCount)
            yield 1
        }

        do! ts |> TaskSeq.iter ignore
        do! ts |> TaskSeq.iter ignore
        do! ts |> TaskSeq.iter ignore
        disposeCount.Value |> should equal 3
    }

    [<Fact>]
    let ``CE taskSeq: use! - DisposeAsync called exactly once per full iteration`` () = task {
        let disposeCount = ref 0

        let ts = taskSeq {
            use! _ = task { return new CountingAsyncDisposable(disposeCount) }
            yield 1
        }

        do! ts |> TaskSeq.iter ignore
        disposeCount.Value |> should equal 1
    }

    [<Fact>]
    let ``CE taskSeq: use! - DisposeAsync called on each re-iteration`` () = task {
        let disposeCount = ref 0

        let ts = taskSeq {
            use! _ = task { return new CountingAsyncDisposable(disposeCount) }
            yield 1
        }

        do! ts |> TaskSeq.iter ignore
        do! ts |> TaskSeq.iter ignore
        do! ts |> TaskSeq.iter ignore
        disposeCount.Value |> should equal 3
    }

    [<Fact>]
    let ``CE taskSeq: use - Dispose called on early termination via take`` () = task {
        let disposeCount = ref 0

        let ts = taskSeq {
            use _ = new CountingDisposable(disposeCount)
            yield 1
            yield 2
            yield 3
        }

        // Only take 1 item — enumerator is disposed before the rest of the sequence runs
        do! ts |> TaskSeq.take 1 |> TaskSeq.iter ignore
        disposeCount.Value |> should equal 1
    }

    [<Fact>]
    let ``CE taskSeq: use - multiple use bindings each get their own Dispose`` () = task {
        let disposeCount = ref 0

        let ts = taskSeq {
            use _ = new CountingDisposable(disposeCount)
            use _ = new CountingDisposable(disposeCount)
            yield 1
        }

        do! ts |> TaskSeq.iter ignore
        disposeCount.Value |> should equal 2
    }

    [<Fact>]
    let ``CE taskSeq: use - each re-iteration creates and disposes a fresh resource`` () = task {
        let createCount = ref 0

        let ts = taskSeq {
            createCount.Value <- createCount.Value + 1
            use _ = new CountingDisposable(ref 0) // fresh ref each time
            yield createCount.Value
        }

        let! first = ts |> TaskSeq.toListAsync
        let! second = ts |> TaskSeq.toListAsync

        // Each re-iteration re-runs the CE body and creates a new resource
        first |> should equal [ 1 ]
        second |> should equal [ 2 ]
        createCount.Value |> should equal 2
    }
