namespace FSharpy

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

    /// <summary>
    /// Returns the first element of the <see cref="IAsyncEnumerable" />, or <see cref="None" /> if the sequence is empty.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    val tryHead: taskSeq: TaskSeqBuilders.taskSeq<'a> -> System.Threading.Tasks.Task<'a option>

    /// <summary>
    /// Returns the first element of the <see cref="IAsyncEnumerable" />.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    val head: taskSeq: TaskSeqBuilders.taskSeq<unit> -> System.Threading.Tasks.Task<unit>

    /// <summary>
    /// Returns the last element of the <see cref="IAsyncEnumerable" />, or <see cref="None" /> if the sequence is empty.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    val tryLast: taskSeq: TaskSeqBuilders.taskSeq<'a> -> System.Threading.Tasks.Task<'a option>

    /// <summary>
    /// Returns the last element of the <see cref="IAsyncEnumerable" />.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    val last: taskSeq: TaskSeqBuilders.taskSeq<unit> -> System.Threading.Tasks.Task<unit>

    /// <summary>
    /// Returns the nth element of the <see cref="IAsyncEnumerable" />, or <see cref="None" /> if the sequence
    /// does not contain enough elements, or if <paramref name="index" /> is negative.
    /// Parameter <paramref name="index" /> is zero-based, that is, the value 0 returns the first element.
    /// </summary>
    val tryItem: index: int -> taskSeq: TaskSeqBuilders.taskSeq<'a> -> System.Threading.Tasks.Task<'a option>

    /// <summary>
    /// Returns the nth element of the <see cref="IAsyncEnumerable" />, or <see cref="None" /> if the sequence
    /// does not contain enough elements, or if <paramref name="index" /> is negative.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence has insufficient length or
    /// <paramref name="index" /> is negative.</exception>
    val item: index: int -> taskSeq: TaskSeqBuilders.taskSeq<unit> -> System.Threading.Tasks.Task<unit>

    /// <summary>
    /// Zips two task sequences, returning a taskSeq of the tuples of each sequence, in order. May raise ArgumentException
    /// if the sequences are or unequal length.
    /// </summary>
    /// <exception cref="ArgumentException">The sequences have different lengths.</exception>
    val zip:
        taskSeq1: TaskSeqBuilders.taskSeq<'T> ->
        taskSeq2: TaskSeqBuilders.taskSeq<'U> ->
            System.Collections.Generic.IAsyncEnumerable<'T * 'U>

    /// Applies a function to each element of the task sequence, threading an accumulator argument through the computation.
    val fold: folder: ('State -> 'T -> 'State) -> state: 'State -> taskSeq: taskSeq<'T> -> Task<'State>

    /// Applies an async function to each element of the task sequence, threading an accumulator argument through the computation.
    val foldAsync: folder: ('State -> 'T -> #Task<'State>) -> state: 'State -> taskSeq: taskSeq<'T> -> Task<'State>
