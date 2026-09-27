module TaskSeq.Tests.Utils

#nowarn "44" // deprecated aliases (ValueTask.FromResult / ofIValueTaskSource) intentionally under test

open System
open System.Threading.Tasks
open System.Threading.Tasks.Sources
open Xunit
open FsUnit.Xunit

open FSharp.Control


module ValueTaskExtensions =
    [<Fact>]
    let ``ValueTask.CompletedTask is already completed successfully`` () =
        let vt = ValueTask.CompletedTask
        vt.IsCompletedSuccessfully |> should equal true


module ValueTaskConstants =
    [<Fact>]
    let ``ValueTask.True is a completed ValueTask with value true`` () = task {
        ValueTask.True.IsCompletedSuccessfully |> should equal true
        let! result = ValueTask.True
        result |> should equal true
    }

    [<Fact>]
    let ``ValueTask.False is a completed ValueTask with value false`` () = task {
        ValueTask.False.IsCompletedSuccessfully |> should equal true
        let! result = ValueTask.False
        result |> should equal false
    }


module ValueTaskFromResult =
    [<Fact>]
    let ``ValueTask.fromResult creates an already-completed ValueTask with the given value`` () = task {
        let vt = ValueTask.fromResult 42
        vt.IsCompletedSuccessfully |> should equal true
        let! result = vt
        result |> should equal 42
    }

    [<Fact>]
    let ``ValueTask.FromResult (deprecated alias) behaves the same as fromResult`` () = task {
        let vt = ValueTask.FromResult "hello"
        let! result = vt
        result |> should equal "hello"
    }


module ValueTaskOfTask =
    [<Fact>]
    let ``ValueTask.ofTask wraps an already-completed Task<'T>`` () = task {
        let source = Task.FromResult 7
        let vt = ValueTask.ofTask source
        let! result = vt
        result |> should equal 7
    }

    [<Fact>]
    let ``ValueTask.ofTask wraps a not-yet-completed Task<'T>`` () = task {
        let source = task {
            do! Task.Delay 1
            return 99
        }

        let vt = ValueTask.ofTask source
        let! result = vt
        result |> should equal 99
    }


module ValueTaskIgnore =
    [<Fact>]
    let ``ValueTask.ignore on an already-completed ValueTask discards the result`` () =
        let vt = ValueTask.fromResult 123
        let ignored: ValueTask = ValueTask.ignore vt
        ignored.IsCompletedSuccessfully |> should equal true

    [<Fact>]
    let ``ValueTask.ignore still awaits and surfaces exceptions from a non-completed ValueTask`` () = task {
        let source =
            ValueTask<int>(
                task {
                    do! Task.Delay 1
                    return raise (InvalidOperationException "boom")
                }
            )

        let ignored: ValueTask = ValueTask.ignore source

        let run () = task { do! ignored }

        let! ex = Assert.ThrowsAsync<InvalidOperationException>(fun () -> run () :> Task)
        ex.Message |> should equal "boom"
    }

    [<Fact>]
    let ``ValueTask.ignore on a not-yet-completed ValueTask still awaits to completion`` () = task {
        let mutable sideEffect = 0

        let source =
            ValueTask<int>(
                task {
                    do! Task.Delay 1
                    sideEffect <- 1
                    return 5
                }
            )

        let ignored: ValueTask = ValueTask.ignore source
        do! ignored
        sideEffect |> should equal 1
    }


/// Minimal IValueTaskSource<bool> used to exercise ValueTask.ofSource / ofIValueTaskSource.
type private ManualBoolSource() =
    let mutable core = ManualResetValueTaskSourceCore<bool>()

    member _.Version = core.Version
    member _.SetResult value = core.SetResult value

    interface IValueTaskSource<bool> with
        member _.GetResult version = core.GetResult version
        member _.GetStatus version = core.GetStatus version

        member _.OnCompleted(continuation, state, version, flags) = core.OnCompleted(continuation, state, version, flags)


module ValueTaskOfSource =
    [<Fact>]
    let ``ValueTask.ofSource creates a ValueTask backed by an IValueTaskSource<bool>`` () = task {
        let source = ManualBoolSource()
        source.SetResult true
        let vt = ValueTask.ofSource source source.Version
        let! result = vt
        result |> should equal true
    }

    [<Fact>]
    let ``ValueTask.ofIValueTaskSource (deprecated alias) behaves the same as ofSource`` () = task {
        let source = ManualBoolSource()
        source.SetResult false
        let vt = ValueTask.ofIValueTaskSource source source.Version
        let! result = vt
        result |> should equal false
    }


module AsyncBind =
    [<Fact>]
    let ``Async.bind awaits the async and passes the value to the binder`` () =
        let result =
            async { return 21 }
            |> Async.bind (fun n -> async { return n * 2 })
            |> Async.RunSynchronously

        result |> should equal 42

    [<Fact>]
    let ``Async.bind propagates exceptions from the source async`` () =
        let run () =
            async { return raise (InvalidOperationException "source error") }
            |> Async.bind (fun (_: int) -> async { return 0 })
            |> Async.RunSynchronously

        (fun () -> run () |> ignore)
        |> should throw typeof<InvalidOperationException>

    [<Fact>]
    let ``Async.bind propagates exceptions from the binder`` () =
        let run () =
            async { return 1 }
            |> Async.bind (fun _ -> async { return raise (InvalidOperationException "binder error") })
            |> Async.RunSynchronously

        (fun () -> run () |> ignore)
        |> should throw typeof<InvalidOperationException>

    [<Fact>]
    let ``Async.bind chains correctly`` () =
        let result =
            async { return 1 }
            |> Async.bind (fun n -> async { return n + 10 })
            |> Async.bind (fun n -> async { return n + 100 })
            |> Async.RunSynchronously

        result |> should equal 111

    [<Fact>]
    let ``Async.bind passes the unwrapped value, not the Async wrapper`` () =
        // This test specifically verifies the bug fix: binder receives 'T, not Async<'T>
        let mutable receivedType = typeof<unit>

        async { return 42 }
        |> Async.bind (fun (n: int) ->
            receivedType <- n.GetType()
            async { return () })
        |> Async.RunSynchronously

        receivedType |> should equal typeof<int>


module TaskBind =
    [<Fact>]
    let ``Task.bind awaits the task and passes the value to the binder`` () = task {
        let result =
            task { return 21 }
            |> Task.bind (fun n -> task { return n * 2 })

        let! v = result
        v |> should equal 42
    }

    [<Fact>]
    let ``Task.bind chains correctly`` () = task {
        let result =
            task { return 1 }
            |> Task.bind (fun n -> task { return n + 10 })
            |> Task.bind (fun n -> task { return n + 100 })

        let! v = result
        v |> should equal 111
    }


module AsyncMap =
    [<Fact>]
    let ``Async.map transforms the result`` () =
        let result =
            async { return 21 }
            |> Async.map (fun n -> n * 2)
            |> Async.RunSynchronously

        result |> should equal 42

    [<Fact>]
    let ``Async.map chains correctly`` () =
        let result =
            async { return 1 }
            |> Async.map (fun n -> n + 10)
            |> Async.map (fun n -> n + 100)
            |> Async.RunSynchronously

        result |> should equal 111


module TaskMap =
    [<Fact>]
    let ``Task.map transforms the result`` () = task {
        let result = task { return 21 } |> Task.map (fun n -> n * 2)

        let! v = result
        v |> should equal 42
    }
