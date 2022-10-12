namespace FSharpy.TaskSeq

open System.Threading.Tasks

module Task =
    /// Convert an Async<'T> into a Task<'T>
    let inline ofAsync (async: Async<'T>) = task { return! async }

    /// Convert a unit-task into a Task<unit>
    let inline ofTask (task': Task) = task { do! task' }

    /// Convert a Task<'T> into an Async<'T>
    let inline toAsync (task: Task<'T>) = Async.AwaitTask task

    /// Convert a Task<unit> into a Task
    let inline toTask (task: Task<unit>) = task :> Task

    /// Convert a Task<'T> into a Task, ignoring the result
    let inline ignore (task: Task<'T>) =
        TaskBuilder.task {
            let! _ = task
            return ()
        }
        :> Task

    /// Map a Tas<'T>
    let inline map mapper (task: Task<'T>) : Task<'U> =
        TaskBuilder.task {
            let! result = task
            return mapper result
        }

    /// Bind a Task<'T>
    let inline bind binder (task: Task<'T>) : Task<'U> =
        TaskBuilder.task { return! binder task }

module Async =
    /// Convert an Task<'T> into an Async<'T>
    let inline ofTask (task: Task<'T>) = Async.AwaitTask task

    /// Convert a unit-task into an Async<unit>
    let inline ofUnitTask (task: Task) = Async.AwaitTask task

    /// Convert a Task<'T> into an Async<'T>
    let inline toTask (async: Async<'T>) = task { return! async }

    /// Convert an Async<'T> into an Async<unit>, ignoring the result
    let inline ignore (async': Async<'T>) =
        async {
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
    let inline bind binder (task: Async<'T>) : Async<'U> =
        ExtraTopLevelOperators.async { return! binder task }
