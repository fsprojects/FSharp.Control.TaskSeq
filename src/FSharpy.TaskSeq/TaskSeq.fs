namespace FSharpy

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

module TaskSeq =
    // F# BUG: the following module is 'AutoOpen' and this isn't needed in the Tests project. Why do we need to open it?
    open FSharpy.TaskSeqBuilders

    // Just for convenience
    module Internal = FSharpy.TaskSeqInternal

    /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted.
    let toList (t: taskSeq<'T>) = [
        let e = t.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    ]


    /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted.
    let toArray (taskSeq: taskSeq<'T>) = [|
        let e = taskSeq.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    |]

    /// Initialize an empty taskSeq.
    let empty<'T> = taskSeq {
        for c: 'T in [] do
            yield c
    }

    /// Create a taskSeq of an array.
    let ofArray (array: 'T[]) = taskSeq {
        for c in array do
            yield c
    }

    /// Create a taskSeq of a list.
    let ofList (list: 'T list) = taskSeq {
        for c in list do
            yield c
    }

    /// Create a taskSeq of a seq.
    let ofSeq (sequence: 'T seq) = taskSeq {
        for c in sequence do
            yield c
    }

    /// Create a taskSeq of a ResizeArray, aka List.
    let ofResizeArray (data: 'T ResizeArray) = taskSeq {
        for c in data do
            yield c
    }

    /// Create a taskSeq of a sequence of tasks, that may already have hot-started.
    let ofTaskSeq (sequence: #Task<'T> seq) = taskSeq {
        for c in sequence do
            let! c = c
            yield c
    }

    /// Create a taskSeq of a list of tasks, that may already have hot-started.
    let ofTaskList (list: #Task<'T> list) = taskSeq {
        for c in list do
            let! c = c
            yield c
    }

    /// Create a taskSeq of an array of tasks, that may already have hot-started.
    let ofTaskArray (array: #Task<'T> array) = taskSeq {
        for c in array do
            let! c = c
            yield c
    }

    /// Create a taskSeq of a seq of async.
    let ofAsyncSeq (sequence: Async<'T> seq) = taskSeq {
        for c in sequence do
            let! c = task { return! c }
            yield c
    }

    /// Create a taskSeq of a list of async.
    let ofAsyncList (list: Async<'T> list) = taskSeq {
        for c in list do
            let! c = Task.ofAsync c
            yield c
    }

    /// Create a taskSeq of an array of async.
    let ofAsyncArray (array: Async<'T> array) = taskSeq {
        for c in array do
            let! c = Async.toTask c
            yield c
    }

    //
    // Convert 'To' functions
    //

    /// Unwraps the taskSeq as a Task<array<_>>. This function is non-blocking.
    let toArrayAsync taskSeq =
        Internal.toResizeArrayAsync taskSeq
        |> Task.map (fun a -> a.ToArray())

    /// Unwraps the taskSeq as a Task<list<_>>. This function is non-blocking.
    let toListAsync taskSeq = Internal.toResizeArrayAndMapAsync List.ofSeq taskSeq

    /// Unwraps the taskSeq as a Task<ResizeArray<_>>. This function is non-blocking.
    let toResizeArrayAsync taskSeq = Internal.toResizeArrayAsync taskSeq

    /// Unwraps the taskSeq as a Task<IList<_>>. This function is non-blocking.
    let toIListAsync taskSeq = Internal.toResizeArrayAndMapAsync (fun x -> x :> IList<_>) taskSeq

    /// Unwraps the taskSeq as a Task<seq<_>>. This function is non-blocking,
    /// exhausts the sequence and caches the results of the tasks in the sequence.
    let toSeqCachedAsync taskSeq = Internal.toResizeArrayAndMapAsync (fun x -> x :> seq<_>) taskSeq

    //
    // iter/map/collect functions
    //

    /// Iterates over the taskSeq. This function is non-blocking
    /// exhausts the sequence as soon as the task is evaluated.
    let iter action taskSeq = Internal.iter (SimpleAction action) taskSeq

    /// Iterates over the taskSeq. This function is non-blocking,
    /// exhausts the sequence as soon as the task is evaluated.
    let iteri action taskSeq = Internal.iter (CountableAction action) taskSeq

    /// Iterates over the taskSeq. This function is non-blocking
    /// exhausts the sequence as soon as the task is evaluated.
    let iterAsync action taskSeq = Internal.iter (AsyncSimpleAction action) taskSeq

    /// Iterates over the taskSeq. This function is non-blocking,
    /// exhausts the sequence as soon as the task is evaluated.
    let iteriAsync action taskSeq = Internal.iter (AsyncCountableAction action) taskSeq

    /// Maps over the taskSeq. This function is non-blocking.
    let map (mapper: 'T -> 'U) taskSeq = Internal.map (SimpleAction mapper) taskSeq

    /// Maps over the taskSeq with an index. This function is non-blocking.
    let mapi (mapper: int -> 'T -> 'U) taskSeq = Internal.map (CountableAction mapper) taskSeq

    /// Maps over the taskSeq. This function is non-blocking.
    let mapAsync mapper taskSeq = Internal.map (AsyncSimpleAction mapper) taskSeq

    /// Maps over the taskSeq with an index. This function is non-blocking.
    let mapiAsync mapper taskSeq = Internal.map (AsyncCountableAction mapper) taskSeq

    /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
    let collect (binder: 'T -> #IAsyncEnumerable<'U>) taskSeq = Internal.collect binder taskSeq

    /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
    let collectSeq (binder: 'T -> #seq<'U>) taskSeq = Internal.collectSeq binder taskSeq

    //
    // zip/unzip etc functions
    //

    /// Zips two task sequences, returning a taskSeq of the tuples of each sequence, in order. May raise ArgumentException
    /// if the sequences are or unequal length.
    let zip taskSeq1 taskSeq2 = Internal.zip taskSeq1 taskSeq2
