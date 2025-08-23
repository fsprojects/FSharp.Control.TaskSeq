module TaskSeq.Tests.Utils

open Xunit
open FsUnit.Xunit
open System
open System.Threading.Tasks
open FSharp.Control

//
// Tests for utility functions in Utils.fs
//

module ValueTaskTests =
    [<Fact>]
    let ``ValueTask.False returns false ValueTask`` () = task {
        let! result = ValueTask.False
        result |> should equal false
    }

    [<Fact>]
    let ``ValueTask.True returns true ValueTask`` () = task {
        let! result = ValueTask.True
        result |> should equal true
    }

    [<Fact>]
    let ``ValueTask.fromResult creates ValueTask with correct value`` () = task {
        let! result = ValueTask.fromResult 42
        result |> should equal 42
    }

    [<Fact>]
    let ``ValueTask.fromResult with null value`` () = task {
        let! result = ValueTask.fromResult null
        result |> should equal null
    }

    [<Fact>]
    let ``ValueTask.fromResult with string value`` () = task {
        let! result = ValueTask.fromResult "test"
        result |> should equal "test"
    }

    [<Fact>]
    let ``ValueTask.ofTask converts Task to ValueTask`` () = task {
        let originalTask = Task.FromResult 100
        let valueTask = ValueTask.ofTask originalTask
        let! result = valueTask
        result |> should equal 100
    }

    [<Fact>]
    let ``ValueTask.ignore on completed ValueTask returns empty ValueTask`` () = task {
        let valueTask = ValueTask.fromResult 42
        let ignoredTask = ValueTask.ignore valueTask
        do! ignoredTask
        // Test passes if no exception and task completes
        Assert.True(true)
    }

    [<Fact>]
    let ``ValueTask.ignore on non-completed ValueTask`` () = task {
        let delayedTask = task {
            do! Task.Delay(10)
            return 42
        }
        let valueTask = ValueTask<int>(delayedTask)
        let ignoredTask = ValueTask.ignore valueTask
        do! ignoredTask
        // Test passes if no exception and task completes
        Assert.True(true)
    }

    [<Fact>]
    let ``ValueTask.CompletedTask extension property`` () =
        let completedTask = ValueTask.CompletedTask
        completedTask.IsCompleted |> should be True

    [<Fact>]
    let ``Obsolete ValueTask.FromResult still works`` () = task {
        let! result = ValueTask.FromResult 42
        result |> should equal 42
    }

module TaskModuleTests =
    [<Fact>]
    let ``Task.fromResult creates task with correct value`` () = task {
        let! result = Task.fromResult 42
        result |> should equal 42
    }

    [<Fact>]
    let ``Task.apply applies function and wraps in task`` () = task {
        let func = fun x -> x * 2
        let appliedFunc = Task.apply func
        let! result = appliedFunc 21
        result |> should equal 42
    }

    [<Fact>]
    let ``Task.map transforms task value`` () = task {
        let originalTask = Task.fromResult 21
        let mappedTask = Task.map (fun x -> x * 2) originalTask
        let! result = mappedTask
        result |> should equal 42
    }

    [<Fact>]
    let ``Task.bind chains tasks`` () = task {
        let originalTask = Task.fromResult 21
        let boundTask = Task.bind (fun x -> Task.fromResult (x * 2)) originalTask
        let! result = boundTask
        result |> should equal 42
    }

    [<Fact>]
    let ``Task.ignore discards task result`` () = task {
        let originalTask = Task.fromResult 42
        let ignoredTask = Task.ignore originalTask
        do! ignoredTask
        // Test passes if task completes without error
        Assert.True(true)
    }

    [<Fact>]
    let ``Task.toValueTask converts Task to ValueTask`` () = task {
        let originalTask = Task.fromResult 42
        let valueTask = Task.toValueTask originalTask
        let! result = valueTask
        result |> should equal 42
    }

    [<Fact>]
    let ``Task.ofValueTask converts ValueTask to Task`` () = task {
        let valueTask = ValueTask.fromResult 42
        let convertedTask = Task.ofValueTask valueTask
        let! result = convertedTask
        result |> should equal 42
    }

    [<Fact>]
    let ``Task.ofTask converts Task to Task`` () = task {
        let originalTask = Task.Delay(10)
        let convertedTask = Task.ofTask originalTask
        do! convertedTask
        // Test passes if task completes
        Assert.True(true)
    }

    [<Fact>]
    let ``Task.ofAsync converts Async to Task`` () = task {
        let asyncOperation = async { return 42 }
        let convertedTask = Task.ofAsync asyncOperation
        let! result = convertedTask
        result |> should equal 42
    }

    [<Fact>]
    let ``Task.toAsync converts Task to Async`` () = async {
        let task = Task.fromResult 42
        let asyncOperation = Task.toAsync task
        let! result = asyncOperation
        result |> should equal 42
    }

module AsyncModuleTests =
    [<Fact>]
    let ``Async.ofTask converts Task to Async`` () = async {
        let task = Task.FromResult 42
        let asyncOperation = Async.ofTask task
        let! result = asyncOperation
        result |> should equal 42
    }

    [<Fact>]
    let ``Async.ofUnitTask converts unit Task to Async`` () = async {
        let task = Task.Delay(10)
        let asyncOperation = Async.ofUnitTask task
        do! asyncOperation
        // Test passes if completes without error
        Assert.True(true)
    }

    [<Fact>]
    let ``Async.toTask converts Async to Task`` () = task {
        let asyncOperation = async { return 42 }
        let convertedTask = Async.toTask asyncOperation
        let! result = convertedTask
        result |> should equal 42
    }

    [<Fact>]
    let ``Async.ignore discards async result`` () = async {
        let asyncOperation = async { return 42 }
        let ignoredAsync = Async.ignore asyncOperation
        do! ignoredAsync
        // Test passes if completes without error
        Assert.True(true)
    }

    [<Fact>]
    let ``Async.map transforms async value`` () = async {
        let asyncOperation = async { return 21 }
        let mappedAsync = Async.map (fun x -> x * 2) asyncOperation
        let! result = mappedAsync
        result |> should equal 42
    }

    [<Fact>]
    let ``Async.bind chains async operations`` () = async {
        let asyncOperation = async { return 21 }
        let boundAsync = Async.bind (fun x -> async { return x * 2 }) asyncOperation
        let! result = boundAsync
        result |> should equal 42
    }

module ErrorHandling =
    [<Fact>]
    let ``Task.map handles exceptions properly`` () =
        let faultedTask = Task.FromException<int>(Exception("test error"))
        let mappedTask = Task.map (fun x -> x * 2) faultedTask
        
        task {
            let! ex = Assert.ThrowsAsync<Exception>(fun () -> mappedTask :> Task)
            ex.Message |> should equal "test error"
        }

    [<Fact>]
    let ``Task.bind handles exceptions properly`` () =
        let faultedTask = Task.FromException<int>(Exception("test error"))
        let boundTask = Task.bind (fun x -> Task.fromResult (x * 2)) faultedTask
        
        task {
            let! ex = Assert.ThrowsAsync<Exception>(fun () -> boundTask :> Task)
            ex.Message |> should equal "test error"
        }

    [<Fact>]
    let ``Async.map handles exceptions properly`` () =
        let faultedAsync = async { failwith "test error" }
        let mappedAsync = Async.map (fun x -> x * 2) faultedAsync
        
        async {
            let! ex = Async.Catch mappedAsync
            match ex with
            | Choice2Of2 ex -> ex.Message |> should equal "test error"
            | Choice1Of2 _ -> Assert.Fail("Expected exception")
        }

module ValueTaskExtensionsTests =
    [<Fact>]
    let ``CompletedTask extension is completed`` () =
        let completedTask = ValueTask.CompletedTask
        completedTask.IsCompleted |> should be True
        
    [<Fact>]
    let ``CompletedTask extension can be awaited`` () = task {
        do! ValueTask.CompletedTask
        // Test passes if no exception
        Assert.True(true)
    }
