namespace FSharp.Control

open System.Threading.Tasks
open System
open System.Diagnostics
open System.Threading

[<AutoOpen>]
module ValueTaskExtensions =
    /// Extensions for ValueTask that are not available in NetStandard 2.1, but are
    /// available in .NET 5+. We put them in Extension space to mimic the behavior of NetStandard 2.1
    type ValueTask with

        /// (Extension member) Gets a task that has already completed successfully.
        static member inline CompletedTask = Unchecked.defaultof<ValueTask>


module ValueTask =
    /// A successfully completed ValueTask of boolean that has the value false.
    let False = ValueTask<bool>()

    /// A successfully completed ValueTask of boolean that has the value true.
    let True = ValueTask<bool> true

    /// Creates a ValueTask with the supplied result of the successful operation.
    let inline FromResult (x: 'T) = ValueTask<'T> x

    /// Creates a ValueTask with an IValueTaskSource representing the operation
    let inline ofIValueTaskSource taskSource version = ValueTask<bool>(taskSource, version)

    /// Creates a ValueTask form a Task<'T>
    let inline ofTask (task: Task<'T>) = ValueTask<'T>(task)

    /// Ignore a ValueTask<'T>, returns a non-generic ValueTask.
    let inline ignore (vtask: ValueTask<'T>) =
        // this implementation follows Stephen Toub's advice, see:
        // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
        if vtask.IsCompletedSuccessfully then
            // ensure any side effect executes
            vtask.Result |> ignore
            ValueTask()
        else
            ValueTask(vtask.AsTask())

module Task =
    /// Convert an Async<'T> into a Task<'T>
    let inline ofAsync (async: Async<'T>) = task { return! async }

    /// Convert a unit-task into a Task<unit>
    let inline ofTask (task': Task) = task { do! task' }

    /// Convert a non-task function into a task-returning function
    let inline apply (func: _ -> _) = func >> Task.FromResult

    /// Convert a Task<'T> into an Async<'T>
    let inline toAsync (task: Task<'T>) = Async.AwaitTask task

    /// Convert a Task<'T> into a ValueTask<'T>
    let inline toValueTask (task: Task<'T>) = ValueTask<'T> task

    /// <summary>
    /// Convert a ValueTask&lt;'T> to a Task&lt;'T>. To use a non-generic ValueTask,
    /// consider using: <paramref name="myValueTask |> Task.ofValueTask |> Task.ofTask" />.
    /// </summary>
    let inline ofValueTask (valueTask: ValueTask<'T>) = task { return! valueTask }

    /// Convert a Task<'T> into a non-generic Task, ignoring the result
    let inline ignore (task: Task<'T>) =
        TaskBuilder.task {
            // ensure the task is awaited
            let! _ = task
            return ()
        }
        :> Task

    /// Map a Task<'T>
    let inline map mapper (task: Task<'T>) : Task<'U> =
        TaskBuilder.task {
            let! result = task
            return mapper result
        }

    /// Bind a Task<'T>
    let inline bind binder (task: Task<'T>) : Task<'U> =
        TaskBuilder.task {
            let! t = task
            return! binder t
        }

    /// Create a task from a value
    let inline fromResult (value: 'U) : Task<'U> = TaskBuilder.task { return value }

module Async =
    /// Convert an Task<'T> into an Async<'T>
    let inline ofTask (task: Task<'T>) = Async.AwaitTask task

    /// Convert a unit-task into an Async<unit>
    let inline ofUnitTask (task: Task) = Async.AwaitTask task

    /// Convert a Task<'T> into an Async<'T>
    let inline toTask (async: Async<'T>) = task { return! async }

    /// Convert an Async<'T> into an Async<unit>, ignoring the result
    let inline ignore (async': Async<'T>) = async {
        let! _ = async'
        return ()
    }

    /// Map an Async<'T>
    let inline map mapper (async: Async<'T>) : Async<'U> =
        ExtraTopLevelOperators.async {
            let! result = async
            return mapper result
        }

    /// Bind an Async<'T>
    let inline bind binder (task: Async<'T>) : Async<'U> = ExtraTopLevelOperators.async { return! binder task }
