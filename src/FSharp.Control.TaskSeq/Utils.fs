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
        static member inline CompletedTask =
            // This mimics how it is done in .NET itself
            Unchecked.defaultof<ValueTask>


module ValueTask =
    let False = ValueTask<bool>()
    let True = ValueTask<bool> true
    let inline fromResult (value: 'T) = ValueTask<'T> value
    let inline ofSource taskSource version = ValueTask<bool>(taskSource, version)
    let inline ofTask (task: Task<'T>) = ValueTask<'T> task

    let inline ignore (vtask: ValueTask<'T>) =
        // this implementation follows Stephen Toub's advice, see:
        // https://github.com/dotnet/runtime/issues/31503#issuecomment-554415966
        if vtask.IsCompletedSuccessfully then
            // ensure any side effect executes
            vtask.Result |> ignore
            ValueTask()
        else
            ValueTask(vtask.AsTask())

    [<Obsolete "From version 0.4.0 onward, 'ValueTask.FromResult' is deprecated in favor of 'ValueTask.fromResult'. It will be removed in an upcoming release.">]
    let inline FromResult (value: 'T) = ValueTask<'T> value

    [<Obsolete "From version 0.4.0 onward, 'ValueTask.ofIValueTaskSource' is deprecated in favor of 'ValueTask.ofSource'. It will be removed in an upcoming release.">]
    let inline ofIValueTaskSource taskSource version = ofSource taskSource version


module Task =
    let inline fromResult (value: 'U) : Task<'U> = Task.FromResult value
    let inline ofAsync (async: Async<'T>) = task { return! async }
    let inline ofTask (task': Task) = task { do! task' }
    let inline apply (func: _ -> _) = func >> Task.FromResult
    let inline toAsync (task: Task<'T>) = Async.AwaitTask task
    let inline toValueTask (task: Task<'T>) = ValueTask<'T> task
    let inline ofValueTask (valueTask: ValueTask<'T>) = task { return! valueTask }

    let inline ignore (task: Task<'T>) =
        TaskBuilder.task {
            // ensure the task is awaited
            let! _ = task
            return ()
        }
        :> Task

    let inline map mapper (task: Task<'T>) : Task<'U> = TaskBuilder.task {
        let! result = task
        return mapper result
    }

    let inline bind binder (task: Task<'T>) : Task<'U> = TaskBuilder.task {
        let! t = task
        return! binder t
    }

module Async =
    let inline ofTask (task: Task<'T>) = Async.AwaitTask task
    let inline ofUnitTask (task: Task) = Async.AwaitTask task
    let inline toTask (async: Async<'T>) = task { return! async }
    let inline bind binder (task: Async<'T>) : Async<'U> = ExtraTopLevelOperators.async { return! binder task }

    let inline ignore (async': Async<'T>) = async {
        let! _ = async'
        return ()
    }

    let inline map mapper (async: Async<'T>) : Async<'U> = ExtraTopLevelOperators.async {
        let! result = async
        return mapper result
    }
