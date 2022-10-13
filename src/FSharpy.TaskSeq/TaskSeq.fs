namespace FSharpy

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

module TaskSeq =
    // F# BUG: the following module is 'AutoOpen' and this isn't needed in the Tests project. Why do we need to open it?
    open FSharpy.TaskSeqBuilders

    // Just for convenience
    module Internal = TaskSeqInternal

    /// Initialize an empty taskSeq.
    let empty<'T> = taskSeq {
        for c: 'T in [] do
            yield c
    }


    //
    // Convert 'ToXXX' functions
    //

    /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
    let toList (t: taskSeq<'T>) = [
        let e = t.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    ]


    /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
    let toArray (taskSeq: taskSeq<'T>) = [|
        let e = taskSeq.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    |]

    /// Returns taskSeq as a seq, similar to Seq.cached. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
    let toSeqCached (taskSeq: taskSeq<'T>) = seq {
        let e = taskSeq.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    }

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
    // Convert 'OfXXX' functions
    //

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
    // iter/map/collect functions
    //

    /// Iterates over the taskSeq applying the action function to each item. This function is non-blocking
    /// exhausts the sequence as soon as the task is evaluated.
    let iter action taskSeq = Internal.iter (SimpleAction action) taskSeq

    /// Iterates over the taskSeq applying the action function to each item. This function is non-blocking,
    /// exhausts the sequence as soon as the task is evaluated.
    let iteri action taskSeq = Internal.iter (CountableAction action) taskSeq

    /// Iterates over the taskSeq applying the async action to each item. This function is non-blocking
    /// exhausts the sequence as soon as the task is evaluated.
    let iterAsync action taskSeq = Internal.iter (AsyncSimpleAction action) taskSeq

    /// Iterates over the taskSeq, applying the async action to each item. This function is non-blocking,
    /// exhausts the sequence as soon as the task is evaluated.
    let iteriAsync action taskSeq = Internal.iter (AsyncCountableAction action) taskSeq

    /// Maps over the taskSeq, applying the mapper function to each item. This function is non-blocking.
    let map (mapper: 'T -> 'U) taskSeq = Internal.map (SimpleAction mapper) taskSeq

    /// Maps over the taskSeq with an index, applying the mapper function to each item. This function is non-blocking.
    let mapi (mapper: int -> 'T -> 'U) taskSeq = Internal.map (CountableAction mapper) taskSeq

    /// Maps over the taskSeq, applying the async mapper function to each item. This function is non-blocking.
    let mapAsync mapper taskSeq = Internal.map (AsyncSimpleAction mapper) taskSeq

    /// Maps over the taskSeq with an index, applying the async mapper function to each item. This function is non-blocking.
    let mapiAsync mapper taskSeq = Internal.map (AsyncCountableAction mapper) taskSeq

    /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
    let collect (binder: 'T -> #IAsyncEnumerable<'U>) taskSeq = Internal.collect binder taskSeq

    /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
    let collectSeq (binder: 'T -> #seq<'U>) taskSeq = Internal.collectSeq binder taskSeq

    /// Applies the given async function to the items in the taskSeq and concatenates all the results in order.
    let collectAsync (binder: 'T -> #Task<#IAsyncEnumerable<'U>>) taskSeq : taskSeq<'U> =
        Internal.collectAsync binder taskSeq

    /// Applies the given async function to the items in the taskSeq and concatenates all the results in order.
    let collectSeqAsync (binder: 'T -> #Task<#seq<'U>>) taskSeq : taskSeq<'U> = Internal.collectSeqAsync binder taskSeq

    //
    // choosers, pickers and the like
    //

    /// <summary>
    /// Returns the first element of the <see cref="IAsyncEnumerable" />, or <see cref="None" /> if the sequence is empty.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    let tryHead taskSeq = Internal.tryHead taskSeq

    /// <summary>
    /// Returns the first element of the <see cref="IAsyncEnumerable" />.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    let head taskSeq = task {
        match! Internal.tryHead taskSeq with
        | Some head -> head
        | None -> Internal.raiseEmptySeq ()
    }

    /// <summary>
    /// Returns the last element of the <see cref="IAsyncEnumerable" />, or <see cref="None" /> if the sequence is empty.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    let tryLast taskSeq = Internal.tryLast taskSeq

    /// <summary>
    /// Returns the last element of the <see cref="IAsyncEnumerable" />.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    let last taskSeq = task {
        match! Internal.tryLast taskSeq with
        | Some last -> last
        | None -> Internal.raiseEmptySeq ()
    }

    /// <summary>
    /// Returns the nth element of the <see cref="IAsyncEnumerable" />, or <see cref="None" /> if the sequence
    /// does not contain enough elements, or if <paramref name="index" /> is negative.
    /// Parameter <paramref name="index" /> is zero-based, that is, the value 0 returns the first element.
    /// </summary>
    let tryItem index taskSeq = Internal.tryItem index taskSeq

    /// <summary>
    /// Returns the nth element of the <see cref="IAsyncEnumerable" />, or <see cref="None" /> if the sequence
    /// does not contain enough elements, or if <paramref name="index" /> is negative.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence has insufficient length or
    /// <paramref name="index" /> is negative.</exception>
    let item index taskSeq = task {
        match! Internal.tryItem index taskSeq with
        | Some item -> item
        | None -> Internal.raiseInsufficient ()
    }

    //
    // zip/unzip etc functions
    //

    /// <summary>
    /// Zips two task sequences, returning a taskSeq of the tuples of each sequence, in order. May raise ArgumentException
    /// if the sequences are or unequal length.
    /// </summary>
    /// <exception cref="ArgumentException">The sequences have different lengths.</exception>
    let zip taskSeq1 taskSeq2 = Internal.zip taskSeq1 taskSeq2

    /// <summary>
    /// Applies a function to each element of the task sequence, threading an accumulator argument through the computation.
    /// </summary>
    let fold folder state taskSeq = Internal.fold (FolderAction folder) state taskSeq

    /// Applies an async function to each element of the task sequence, threading an accumulator argument through the computation.
    let foldAsync folder state taskSeq = Internal.fold (AsyncFolderAction folder) state taskSeq
