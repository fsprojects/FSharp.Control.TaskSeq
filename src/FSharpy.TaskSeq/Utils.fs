namespace FSharpy

open System.Threading.Tasks

module Task =
    /// Convert an Async<'T> into a Task<'T>
    let inline ofAsync (async: Async<'T>) = task { return! async }

    /// Convert a unit-task into a Task<unit>
    let inline ofTask (task': Task) = task { do! task' }

    /// Convert a non-task function into a task-returning function
    let inline apply (func: _ -> _) = func >> Task.FromResult

    /// Convert a Task<'T> into an Async<'T>
    let inline toAsync (task: Task<'T>) = Async.AwaitTask task

    /// Convert a Task<unit> into a Task
    let inline toTask (task: Task<unit>) = task :> Task

    /// Convert a Task<'T> into a ValueTask<'T>
    let inline toValueTask (task: Task<'T>) = ValueTask<'T> task

    /// Convert a Task<unit> into a non-generic ValueTask
    let inline toIgnoreValueTask (task: Task<unit>) = ValueTask(task :> Task)

    /// <summary>
    /// Convert a ValueTask&lt;'T> to a Task&lt;'T>. To use a non-generic ValueTask,
    /// consider using: <paramref name="myValueTask |> Task.ofValueTask |> Task.ofTask" />.
    /// </summary>
    let inline ofValueTask (valueTask: ValueTask<'T>) = task { return! valueTask }

    /// Convert a Task<'T> into a Task, ignoring the result
    let inline ignore (task: Task<'T>) =
        TaskBuilder.task {
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
    let inline bind binder (task: Task<'T>) : Task<'U> = TaskBuilder.task { return! binder task }

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
