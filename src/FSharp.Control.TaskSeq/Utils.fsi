namespace FSharp.Control

open System
open System.Threading.Tasks
open System.Threading.Tasks.Sources

[<AutoOpen>]
module ValueTaskExtensions =

    /// Shims back-filling .NET 5+ functionality for use on netstandard2.1
    type ValueTask with

        /// (Extension member) Gets a ValueTask that has already completed successfully.
        static member inline CompletedTask: ValueTask

module ValueTask =

    /// A successfully completed ValueTask of boolean that has the value false.
    val False: ValueTask<bool>

    /// A successfully completed ValueTask of boolean that has the value true.
    val True: ValueTask<bool>

    /// Creates a ValueTask with the supplied result of the successful operation.
    val inline fromResult: value: 'T -> ValueTask<'T>

    /// <summary>
    /// The function <paramref name="FromResult" /> is deprecated since version 0.4.0,
    /// please use <paramref name="fromResult" /> in its stead. See <see cref="T:FSharp.Control.ValueTask.fromResult" />.
    /// </summary>
    [<Obsolete "From version 0.4.0 onward, 'ValueTask.FromResult' is deprecated in favor of 'ValueTask.fromResult'. It will be removed in an upcoming release.">]
    val inline FromResult: value: 'T -> ValueTask<'T>

    /// <summary>
    /// Initializes a new instance of <see cref="ValueTask" /> with an <see cref="IValueTaskSource" />
    /// representing its operation.
    /// </summary>
    val inline ofSource: taskSource: IValueTaskSource<bool> -> version: int16 -> ValueTask<bool>

    /// <summary>
    /// The function <paramref name="ofIValueTaskSource" /> is deprecated since version 0.4.0,
    /// please use <paramref name="ofSource" /> in its stead. See <see cref="T:FSharp.Control.ValueTask.ofSource" />.
    /// </summary>
    [<Obsolete "From version 0.4.0 onward, 'ValueTask.ofIValueTaskSource' is deprecated in favor of 'ValueTask.ofSource'. It will be removed in an upcoming release.">]
    val inline ofIValueTaskSource: taskSource: IValueTaskSource<bool> -> version: int16 -> ValueTask<bool>

    /// Creates a ValueTask from a Task<'T>
    val inline ofTask: task: Task<'T> -> ValueTask<'T>

    /// Convert a ValueTask<'T> into a non-generic ValueTask, ignoring the result
    val inline ignore: valueTask: ValueTask<'T> -> ValueTask

module Task =
    /// A successfully completed Task of boolean that has the value false.
    val False: Task<bool>

    /// A successfully completed Task of boolean that has the value true.
    val True: Task<bool>

    /// Creates a Task<'U> that's completed successfully with the specified result.
    val inline fromResult: value: 'U -> Task<'U>

    /// Starts the `Async<'T>` computation, returning the associated `Task<'T>`
    val inline ofAsync: async: Async<'T> -> Task<'T>

    /// Convert a non-generic Task into a Task<unit>
    val inline ofTask: task': Task -> Task<unit>

    /// Convert a plain function into a task-returning function
    val inline apply: func: ('a -> 'b) -> ('a -> Task<'b>)

    /// Convert a Task<'T> into an Async<'T>
    val inline toAsync: task: Task<'T> -> Async<'T>

    /// Convert a Task<'T> into a ValueTask<'T>
    val inline toValueTask: task: Task<'T> -> ValueTask<'T>

    /// <summary>
    /// Convert a ValueTask&lt;'T> to a Task&lt;'T>. For a non-generic ValueTask,
    /// consider: <paramref name="myValueTask |> Task.ofValueTask |> Task.ofTask" />.
    /// </summary>
    val inline ofValueTask: valueTask: ValueTask<'T> -> Task<'T>

    /// Convert a Task<'T> into a non-generic Task, ignoring the result
    val inline ignore: task: Task<'T> -> Task

    /// Map a Task<'T>
    val inline map: mapper: ('T -> 'U) -> task: Task<'T> -> Task<'U>

    /// Bind a Task<'T>
    val inline bind: binder: ('T -> #Task<'U>) -> task: Task<'T> -> Task<'U>

module Async =

    /// Convert an Task<'T> into an Async<'T>
    val inline ofTask: task: Task<'T> -> Async<'T>

    /// Convert a non-generic Task into an Async<unit>
    val inline ofUnitTask: task: Task -> Async<unit>

    /// Starts the `Async<'T>` computation, returning the associated `Task<'T>`
    val inline toTask: async: Async<'T> -> Task<'T>

    /// Convert an Async<'T> into an Async<unit>, ignoring the result
    val inline ignore: async: Async<'T> -> Async<unit>

    /// Map an Async<'T>
    val inline map: mapper: ('T -> 'U) -> async: Async<'T> -> Async<'U>

    /// Bind an Async<'T>
    val inline bind: binder: (Async<'T> -> Async<'U>) -> async: Async<'T> -> Async<'U>
