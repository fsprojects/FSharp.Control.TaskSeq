# TaskSeq
An implementation [`IAsyncEnumerable<'T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=net-7.0) as a `taskSeq` CE for F# with accompanying `TaskSeq` module.

The `IAsyncEnumerable` interface was added to .NET in `.NET Core 3.0` and is part of `.NET Standard 2.1`. The main use-case was for iterative asynchronous enumeration over some resource. For instance, an event stream or a REST API interface with pagination, where each page is a [`MoveNextAsync`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerator-1.movenextasync?view=net-7.0) call on the [`IAsyncEnumerator<'T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerator-1?view=net-7.0) given by a call to [`GetAsyncEnumerator()`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1.getasyncenumerator?view=net-7.0). It has been relatively challenging to work properly with this type and dealing with each step being asynchronous, and the enumerator implementing [`IAsyncDisposable`](https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable?view=net-7.0) as well, which requires careful handling.

A good C#-based introduction on `IAsyncEnumerable` [can be found in this blog](https://stu.dev/iasyncenumerable-introduction/). Another resource is [this MSDN article shortly after its introductiono](https://learn.microsoft.com/en-us/archive/msdn-magazine/2019/november/csharp-iterating-with-async-enumerables-in-csharp-8).

## In progress!!!

It's based on [Don Symes `taskSeq.fs`](https://github.com/dotnet/fsharp/blob/d5312aae8aad650f0043f055bb14c3aa8117e12e/tests/benchmarks/CompiledCodeBenchmarks/TaskPerf/TaskPerf/taskSeq.fs)
but expanded with useful utility functions and a few extra binding overloads.

## Short-term feature planning

Not necessarily in order of importance:

 - [x] A minimal base set of useful functions and sensible CE overloads, like `map`, `collect`, `fold`, `zip`. These functions will live in the module `TaskSeq`. The CE will be called `taskSeq`.
 - [ ] Packaging and publishing on Nuget
 - [ ] Provide the same surface area of functions as `Seq` in F# Core
 - [ ] For each function, have a "normal" function, where the operator is non-async, and an async version. I.e., `TaskSeq.map` and `TaskSeq.mapAsync`, the difference being that the `mapper` function returns a `#Task<'T>` in the second version.
 - [ ] Examples, documentation and tests
 - [ ] Expand surface area based on user requests
 - [ ] Improving the original code, adding benchmarks, and what have you.

## Current set of `TaskSeq` utility functions

The following is the current surface area of the `TaskSeq` utility functions. This is just a dump of the signatures, proper 
documentation will be added soon(ish):

```f#
module TaskSeq =
    open System.Collections.Generic
    open System.Threading.Tasks
    open FSharpy.TaskSeqBuilders

    /// Initialize an empty taskSeq.
    val empty<'T> : taskSeq<'T>

    /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
    val toList: t: taskSeq<'T> -> 'T list

    /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
    val toArray: taskSeq: taskSeq<'T> -> 'T[]

    /// Returns taskSeq as a seq, similar to Seq.cached. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
    val toSeqCached: taskSeq: taskSeq<'T> -> seq<'T>

    /// Unwraps the taskSeq as a Task<array<_>>. This function is non-blocking.
    val toArrayAsync: taskSeq: taskSeq<'T> -> Task<'T[]>

    /// Unwraps the taskSeq as a Task<list<_>>. This function is non-blocking.
    val toListAsync: taskSeq: taskSeq<'T> -> Task<'T list>

    /// Unwraps the taskSeq as a Task<ResizeArray<_>>. This function is non-blocking.
    val toResizeArrayAsync: taskSeq: taskSeq<'T> -> Task<ResizeArray<'T>>

    /// Unwraps the taskSeq as a Task<IList<_>>. This function is non-blocking.
    val toIListAsync: taskSeq: taskSeq<'T> -> Task<IList<'T>>

    /// Unwraps the taskSeq as a Task<seq<_>>. This function is non-blocking,
    /// exhausts the sequence and caches the results of the tasks in the sequence.
    val toSeqCachedAsync: taskSeq: taskSeq<'T> -> Task<seq<'T>>

    /// Create a taskSeq of an array.
    val ofArray: array: 'T[] -> taskSeq<'T>

    /// Create a taskSeq of a list.
    val ofList: list: 'T list -> taskSeq<'T>

    /// Create a taskSeq of a seq.
    val ofSeq: sequence: seq<'T> -> taskSeq<'T>

    /// Create a taskSeq of a ResizeArray, aka List.
    val ofResizeArray: data: ResizeArray<'T> -> taskSeq<'T>

    /// Create a taskSeq of a sequence of tasks, that may already have hot-started.
    val ofTaskSeq: sequence: seq<#Task<'T>> -> taskSeq<'T>

    /// Create a taskSeq of a list of tasks, that may already have hot-started.
    val ofTaskList: list: #Task<'T> list -> taskSeq<'T>

    /// Create a taskSeq of an array of tasks, that may already have hot-started.
    val ofTaskArray: array: #Task<'T> array -> taskSeq<'T>

    /// Create a taskSeq of a seq of async.
    val ofAsyncSeq: sequence: seq<Async<'T>> -> taskSeq<'T>

    /// Create a taskSeq of a list of async.
    val ofAsyncList: list: Async<'T> list -> taskSeq<'T>

    /// Create a taskSeq of an array of async.
    val ofAsyncArray: array: Async<'T> array -> taskSeq<'T>

    /// Iterates over the taskSeq applying the action function to each item. This function is non-blocking
    /// exhausts the sequence as soon as the task is evaluated.
    val iter: action: ('T -> unit) -> taskSeq: taskSeq<'T> -> Task<unit>

    /// Iterates over the taskSeq applying the action function to each item. This function is non-blocking,
    /// exhausts the sequence as soon as the task is evaluated.
    val iteri: action: (int -> 'T -> unit) -> taskSeq: taskSeq<'T> -> Task<unit>

    /// Iterates over the taskSeq applying the async action to each item. This function is non-blocking
    /// exhausts the sequence as soon as the task is evaluated.
    val iterAsync: action: ('T -> #Task<unit>) -> taskSeq: taskSeq<'T> -> Task<unit>

    /// Iterates over the taskSeq, applying the async action to each item. This function is non-blocking,
    /// exhausts the sequence as soon as the task is evaluated.
    val iteriAsync: action: (int -> 'T -> #Task<unit>) -> taskSeq: taskSeq<'T> -> Task<unit>

    /// Maps over the taskSeq, applying the mapper function to each item. This function is non-blocking.
    val map: mapper: ('T -> 'U) -> taskSeq: taskSeq<'T> -> taskSeq<'U>

    /// Maps over the taskSeq with an index, applying the mapper function to each item. This function is non-blocking.
    val mapi: mapper: (int -> 'T -> 'U) -> taskSeq: taskSeq<'T> -> taskSeq<'U>

    /// Maps over the taskSeq, applying the async mapper function to each item. This function is non-blocking.
    val mapAsync: mapper: ('T -> #Task<'U>) -> taskSeq: taskSeq<'T> -> taskSeq<'U>

    /// Maps over the taskSeq with an index, applying the async mapper function to each item. This function is non-blocking.
    val mapiAsync: mapper: (int -> 'T -> #Task<'U>) -> taskSeq: taskSeq<'T> -> taskSeq<'U>

    /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
    val collect: binder: ('T -> #taskSeq<'U>) -> taskSeq: taskSeq<'T> -> taskSeq<'U>

    /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
    val collectSeq: binder: ('T -> #seq<'U>) -> taskSeq: taskSeq<'T> -> taskSeq<'U>

    /// Applies the given async function to the items in the taskSeq and concatenates all the results in order.
    val collectAsync: binder: ('T -> #Task<'TSeqU>) -> taskSeq: taskSeq<'T> -> taskSeq<'U> when 'TSeqU :> taskSeq<'U>

    /// Applies the given async function to the items in the taskSeq and concatenates all the results in order.
    val collectSeqAsync: binder: ('T -> #Task<'SeqU>) -> taskSeq: taskSeq<'T> -> taskSeq<'U> when 'SeqU :> seq<'U>

    /// Zips two task sequences, returning a taskSeq of the tuples of each sequence, in order. May raise ArgumentException
    /// if the sequences are or unequal length.
    val zip: taskSeq1: taskSeq<'T> -> taskSeq2: taskSeq<'U> -> taskSeq<'T * 'U>

    /// Applies a function to each element of the task sequence, threading an accumulator argument through the computation.
    val fold: folder: ('State -> 'T -> 'State) -> state: 'State -> taskSeq: taskSeq<'T> -> Task<'State>

    /// Applies an async function to each element of the task sequence, threading an accumulator argument through the computation.
    val foldAsync: folder: ('State -> 'T -> #Task<'State>) -> state: 'State -> taskSeq: taskSeq<'T> -> Task<'State>
```
