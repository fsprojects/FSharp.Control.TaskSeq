namespace FSharp.Control

open System.Collections.Generic
open System.Threading.Tasks

#nowarn "1204"

[<AutoOpen>]
module TaskSeqExtensions =
    module TaskSeq =
        /// Initialize an empty task sequence.
        val empty<'T> : taskSeq<'T>

[<Sealed; AbstractClass>]
type TaskSeq =

    /// <summary>
    /// Creates a task sequence from <paramref name="value" /> that generates a single element and then ends.
    /// </summary>
    ///
    /// <param name="value">The input item to use as the single item of the task sequence.</param>
    static member singleton: value: 'T -> taskSeq<'T>

    /// <summary>
    /// Returns <see cref="true" /> if the task sequence contains no elements, <see cref="false" /> otherwise.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member isEmpty: source: taskSeq<'T> -> Task<bool>

    /// <summary>
    /// Returns the length of the sequence. This operation requires the whole sequence to be evaluated and
    /// should not be used on potentially infinite sequences, see <see cref="lengthOrMax" /> for an alternative.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member length: source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence, or <paramref name="max" />, whichever comes first. This operation requires the task sequence
    /// to be evaluated ether in full, or until <paramref name="max" /> items have been processed. Use this method instead of
    /// <see cref="TaskSeq.length" /> if you need to limit the number of items evaluated, or if the sequence is potentially infinite.
    /// </summary>
    ///
    /// <param name="max">Limit at which to stop evaluating source items for finding the length.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member lengthOrMax: max: int -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence of all items for which the <paramref name="predicate" /> returns true.
    /// This operation requires the whole sequence to be evaluated and should not be used on potentially infinite sequences.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.lengthByAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function to test whether an item in the input sequence should be included in the count.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member lengthBy: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence of all items for which the <paramref name="predicate" /> returns true.
    /// This operation requires the whole sequence to be evaluated and should not be used on potentially infinite sequences.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.lengthBy" />.
    /// </summary>
    ///
    /// <param name="predicate">A function to test whether an item in the input sequence should be included in the count.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member lengthByAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns a task sequence that is given by the delayed specification of a task sequence.
    /// </summary>
    ///
    /// <param name="generator">The generating function for the task sequence.</param>
    /// <returns>The generated task sequence.</returns>
    static member delay: generator: (unit -> taskSeq<'T>) -> taskSeq<'T>

    /// <summary>
    /// Generates a new task sequence which, when iterated, will return successive elements by calling the given function
    /// with the curren zero-basedt index, up to the given count. Each element is saved after its initialization for successive access to
    /// <see cref="IAsyncEnumerator.Current" />, which will not re-evaluate the <paramref name="initializer" />. However,
    /// re-iterating the returned task sequence will re-evaluate the initialization function. The returned sequence may
    /// be passed between threads safely. However, individual IEnumerator values generated from the returned sequence should
    /// not be accessed concurrently.
    /// </summary>
    ///
    /// <param name="count">The maximum number of items to generate for the sequence.</param>
    /// <param name="initializer">A function that generates an item in the sequence from a given index.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:System.ArgumentException">Thrown when count is negative.</exception>
    static member init: count: int -> initializer: (int -> 'T) -> taskSeq<'T>

    /// <summary>
    /// Generates a new task sequence which, when iterated, will return successive elements by calling the given function
    /// with the current zero-based index, up to the given count. Each element is saved after its initialization for successive access to
    /// <see cref="IAsyncEnumerator.Current" />, which will not re-evaluate the <paramref name="initializer" />. However,
    /// re-iterating the returned task sequence will re-evaluate the initialization function. The returned sequence may
    /// be passed between threads safely. However, individual IEnumerator values generated from the returned sequence should
    /// not be accessed concurrently.
    /// </summary>
    ///
    /// <param name="count">The maximum number of items to generate for the sequence.</param>
    /// <param name="initializer">A function that generates an item in the sequence from a given index.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:System.ArgumentException">Thrown when count is negative.</exception>
    static member initAsync: count: int -> initializer: (int -> #Task<'T>) -> taskSeq<'T>

    /// <summary>
    /// Generates a new task sequence which, when iterated, will return successive elements by calling the given function
    /// with the current zero-based index, ad infinitum, or until <see cref="Int32.MaxValue" /> is reached.
    /// Each element is saved after its initialization for successive access to
    /// <see cref="IAsyncEnumerator.Current" />, which will not re-evaluate the <paramref name="initializer" />. However,
    /// re-iterating the returned task sequence will re-evaluate the initialization function. The returned sequence may
    /// be passed between threads safely. However, individual IEnumerator values generated from the returned sequence should
    /// not be accessed concurrently.
    /// </summary>
    ///
    /// <param name="initializer">A function that generates an item in the sequence from a given index.</param>
    /// <returns>The resulting task sequence.</returns>
    static member initInfinite: initializer: (int -> 'T) -> taskSeq<'T>

    /// <summary>
    /// Generates a new task sequence which, when iterated, will return successive elements by calling the given function
    /// with the current zero-based index, ad infinitum, or until <see cref="Int32.MaxValue" /> is reached.
    /// Each element is saved after its initialization for successive access to
    /// <see cref="IAsyncEnumerator.Current" />, which will not re-evaluate the <paramref name="initializer" />. However,
    /// re-iterating the returned task sequence will re-evaluate the initialization function. The returned sequence may
    /// be passed between threads safely. However, individual IEnumerator values generated from the returned sequence should
    /// not be accessed concurrently.
    /// </summary>
    ///
    /// <param name="initializer">A function that generates an item in the sequence from a given index.</param>
    /// <returns>The resulting task sequence.</returns>
    static member initInfiniteAsync: initializer: (int -> #Task<'T>) -> taskSeq<'T>

    /// <summary>
    /// Combines the given task sequence of task sequences and concatenates them end-to-end, to form a
    /// new flattened, single task sequence. Each task sequence is awaited item by item, before the next is iterated.
    /// </summary>
    ///
    /// <param name="sources">The input task-sequence-of-task-sequences.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence of task sequences is null.</exception>
    static member concat: sources: taskSeq<#taskSeq<'T>> -> taskSeq<'T>

    /// <summary>
    /// Concatenates task sequences <paramref name="source1" /> and <paramref name="source2" /> in order as a single
    /// task sequence.
    /// </summary>
    ///
    /// <param name="source1">The first input task sequence.</param>
    /// <param name="source2">The second input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input task sequences is null.</exception>
    static member append: source1: taskSeq<'T> -> source2: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Concatenates a task sequence <paramref name="source1" /> with a non-async F# <see cref="seq" /> in <paramref name="source2" />
    /// and returns a single task sequence.
    /// </summary>
    ///
    /// <param name="source1">The input task sequence.</param>
    /// <param name="source2">The input F# <see cref="seq" /> sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input sequences is null.</exception>
    static member appendSeq: source1: taskSeq<'T> -> source2: seq<'T> -> taskSeq<'T>

    /// <summary>
    /// Concatenates a non-async F# <see cref="seq" /> in <paramref name="source1" /> with a task sequence in <paramref name="source2" />
    /// and returns a single task sequence.
    /// </summary>
    ///
    /// <param name="source1">The input F# <see cref="seq" /> sequence.</param>
    /// <param name="source2">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input sequences is null.</exception>
    static member prependSeq: source1: seq<'T> -> source2: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Builds an F# <see cref="list" /> from the input task sequence in <paramref name="source" />.
    /// This function is blocking until the sequence is exhausted and will then properly dispose of the resources.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting list.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toList: source: taskSeq<'T> -> 'T list

    /// <summary>
    /// Builds an <see cref="array" /> from the input task sequence in <paramref name="source" />.
    /// This function is blocking until the sequence is exhausted and will then properly dispose of the resources.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting array.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toArray: source: taskSeq<'T> -> 'T[]

    /// <summary>
    /// Views the task sequence in <paramref name="source" /> as an F# <see cref="seq" />, that is, an
    /// <see cref="IEnumerable&lt;'T>" />. This function is blocking at each <see cref="yield" /> or call
    /// to <see cref="IEnumerable&lt;'T>/MoveNext()" /> in the resulting sequence.
    /// Resources are disposed when the sequence is disposed, or the sequence is exhausted.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toSeq: source: taskSeq<'T> -> seq<'T>

    /// <summary>
    /// Builds an <see cref="array" /> asynchronously from the input task sequence.
    /// This function is non-blocking while it builds the array.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting array.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toArrayAsync: source: taskSeq<'T> -> Task<'T[]>

    /// <summary>
    /// Builds an F# <see cref="list" /> asynchronously from the input task sequence.
    /// This function is non-blocking while it builds the list.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting list.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toListAsync: source: taskSeq<'T> -> Task<'T list>

    /// <summary>
    /// Gathers items into a ResizeArray (see <see cref="T:System.Collections.Generic.List&lt;_>" />) asynchronously from the input task sequence.
    /// This function is non-blocking while it builds the resizable array.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting resizable array.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toResizeArrayAsync: source: taskSeq<'T> -> Task<ResizeArray<'T>>

    /// <summary>
    /// Builds an <see cref="IList&lt;'T>" /> asynchronously from the input task sequence.
    /// This function is non-blocking while it builds the IList.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting IList interface.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toIListAsync: source: taskSeq<'T> -> Task<IList<'T>>

    /// <summary>
    /// Views the given <see cref="array" /> as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input array.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input array is null.</exception>
    static member ofArray: source: 'T[] -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="list" /> as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input list.</param>
    /// <returns>The resulting task sequence.</returns>
    static member ofList: source: 'T list -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="seq" /> as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member ofSeq: source: seq<'T> -> taskSeq<'T>

    /// <summary>
    /// Views the given resizable array as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input resize array.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input resize array is null.</exception>
    static member ofResizeArray: source: ResizeArray<'T> -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="seq" /> of <see cref="Task" />s as a task sequence, that is, as an
    /// <see cref="IAsyncEnumerable&lt;'T>" />. A sequence of tasks is not the same as a task sequence.
    /// Each task in a sequence of tasks can be run individually and potentially out of order, or with
    /// overlapping side effects, while a task sequence forces awaiting between the items in the sequence,
    /// preventing such overlap to happen.
    /// </summary>
    ///
    /// <param name="source">The input sequence-of-tasks.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member ofTaskSeq: source: seq<#Task<'T>> -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="list" /> of <see cref="Task" />s as a task sequence, that is, as an
    /// <see cref="IAsyncEnumerable&lt;'T>" />. A list of tasks will typically already be hot-started,
    /// as a result, each task can already run and potentially out of order, or with
    /// overlapping side effects, while a task sequence forces awaiting between the items in the sequence,
    /// preventing such overlap to happen. Converting a list of tasks into a task sequence is no guarantee
    /// that overlapping side effects are prevented. Safe for side-effect free tasks.
    /// </summary>
    ///
    /// <param name="source">The input list-of-tasks.</param>
    /// <returns>The resulting task sequence.</returns>
    static member ofTaskList: source: #Task<'T> list -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="array" /> of <see cref="Task" />s as a task sequence, that is, as an
    /// <see cref="IAsyncEnumerable&lt;'T>" />. An array of tasks will typically already be hot-started,
    /// as a result, each task can already run and potentially out of order, or with
    /// overlapping side effects, while a task sequence forces awaiting between the items in the sequence,
    /// preventing such overlap to happen. Converting an array of tasks into a task sequence is no guarantee
    /// that overlapping side effects are prevented. Safe for side-effect free tasks.
    /// </summary>
    ///
    /// <param name="source">The input array-of-tasks.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input array is null.</exception>
    static member ofTaskArray: source: #Task<'T> array -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="seq" /> of <see cref="Async" />s as a task sequence, that is, as an
    /// <see cref="IAsyncEnumerable&lt;'T>" />. A sequence of asyncs is not the same as a task sequence.
    /// Each async computation in a sequence of asyncs can be run individually or in parallel, potentially
    /// with overlapping side effects, while a task sequence forces awaiting between the items in the sequence,
    /// preventing such overlap to happen.
    /// </summary>
    ///
    /// <param name="source">The input sequence-of-asyncs.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member ofAsyncSeq: source: seq<Async<'T>> -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="list" /> of <see cref="Async" />s as a task sequence, that is, as an
    /// <see cref="IAsyncEnumerable&lt;'T>" />. A list of asyncs is not the same as a task sequence.
    /// Each async computation in a list of asyncs can be run individually or in parallel, potentially
    /// with overlapping side effects, while a task sequence forces awaiting between the items in the sequence,
    /// preventing such overlap to happen.
    /// </summary>
    ///
    /// <param name="source">The input list-of-asyncs.</param>
    /// <returns>The resulting task sequence.</returns>
    static member ofAsyncList: source: Async<'T> list -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="array" /> of <see cref="Async" />s as a task sequence, that is, as an
    /// <see cref="IAsyncEnumerable&lt;'T>" />. An array of asyncs is not the same as a task sequence.
    /// Each async computation in an array of asyncs can be run individually or in parallel, potentially
    /// with overlapping side effects, while a task sequence forces awaiting between the items in the sequence,
    /// preventing such overlap to happen.
    /// </summary>
    ///
    /// <param name="source">The input array-of-asyncs.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member ofAsyncArray: source: Async<'T> array -> taskSeq<'T>

    /// <summary>
    /// Views each item in the input task sequence as <see cref="obj" />, boxing value types.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member box: source: taskSeq<'T> -> taskSeq<obj>

    /// <summary>
    /// Unboxes to the target type <see cref="'U" /> each item in the input task sequence.
    /// The target type must be a <see cref="struct" /> or a built-in value type.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="InvalidCastException">Thrown when the function is unable to cast an item to the target type.</exception>
    static member unbox<'U when 'U: struct> : source: taskSeq<obj> -> taskSeq<'U>

    /// <summary>
    /// Casts each item in the untyped input task sequence. If the input sequence contains value types
    /// it is recommended to use <see cref="TaskSeq.unbox" /> instead.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="InvalidCastException">Thrown when the function is unable to cast an item to the target type.</exception>
    static member cast: source: taskSeq<obj> -> taskSeq<'U>

    /// <summary>
    /// Iterates over the input task sequence, applying the <paramref name="action" /> function to each item.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">A function to apply to each element of the task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>A <see cref="unit" /> <see cref="task" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member iter: action: ('T -> unit) -> source: taskSeq<'T> -> Task<unit>

    /// <summary>
    /// Iterates over the input task sequence, applying the <paramref name="action" /> function to each item,
    /// supplying the zero-based index as extra parameter for the <paramref name="action" /> function.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">A function to apply to each element of the task sequence that can also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>A <see cref="unit" /> <see cref="task" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member iteri: action: (int -> 'T -> unit) -> source: taskSeq<'T> -> Task<unit>

    /// <summary>
    /// Iterates over the input task sequence, applying the asynchronous <paramref name="action" /> function to each item.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">An asynchronous function to apply to each element of the task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>A <see cref="unit" /> <see cref="task" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member iterAsync: action: ('T -> #Task<unit>) -> source: taskSeq<'T> -> Task<unit>

    /// <summary>
    /// Iterates over the input task sequence, applying the asynchronous <paramref name="action" /> function to each item,
    /// supplying the zero-based index as extra parameter for the <paramref name="action" /> function.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">An asynchronous function to apply to each element of the task sequence that can also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>A <see cref="unit" /> <see cref="task" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member iteriAsync: action: (int -> 'T -> #Task<unit>) -> source: taskSeq<'T> -> Task<unit>

    /// <summary>
    /// Builds a new task sequence whose elements are the corresponding elements of the input task
    /// sequence <paramref name="source" /> paired with the integer index (from 0) of each element.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence of tuples.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member indexed: source: taskSeq<'T> -> taskSeq<int * 'T>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="action" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapping">A function to transform items from the input task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member map: mapper: ('T -> 'U) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="action" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, passing
    /// an extra zero-based index argument to the <paramref name="action" /> function.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapping">A function to transform items from the input task sequence that also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member mapi: mapper: (int -> 'T -> 'U) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="action" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapping">An asynchronous function to transform items from the input task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member mapAsync: mapper: ('T -> #Task<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="action" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, passing
    /// an extra zero-based index argument to the <paramref name="action" /> function.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapping">An asynchronous function to transform items from the input task sequence that also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member mapiAsync: mapper: (int -> 'T -> #Task<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned task sequences.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="binder">A function to transform items from the input task sequence into a task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member collect: binder: ('T -> #taskSeq<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned regular F# sequences.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="binder">A function to transform items from the input task sequence into a regular sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member collectSeq: binder: ('T -> #seq<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned task sequences.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="binder">An asynchronous function to transform items from the input task sequence into a task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member collectAsync:
        binder: ('T -> #Task<'TSeqU>) -> source: taskSeq<'T> -> taskSeq<'U> when 'TSeqU :> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned regular F# sequences.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="binder">An asynchronous function to transform items from the input task sequence into a regular sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member collectSeqAsync:
        binder: ('T -> #Task<'SeqU>) -> source: taskSeq<'T> -> taskSeq<'U> when 'SeqU :> seq<'U>

    /// <summary>
    /// Returns the first element of the input task sequence given by <paramref name="source" />,
    /// or <see cref="None" /> if the sequence is empty.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The first element of the task sequence, or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryHead: source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the first element of the input task sequence given by <paramref name="source" />.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The first element of the task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the task sequence is empty.</exception>
    static member head: source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the whole input task sequence given by <paramref name="source" />, minus its first element,
    /// or <see cref="None" /> if the sequence is empty.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The input task sequence minus the first element, or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryTail: source: taskSeq<'T> -> Task<taskSeq<'T> option>

    /// <summary>
    /// Returns the whole task sequence from <paramref name="source" />, minus its first element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The input task sequence minus the first element.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the task sequence is empty.</exception>
    static member tail: source: taskSeq<'T> -> Task<taskSeq<'T>>

    /// <summary>
    /// Returns the last element of the input task sequence given by <paramref name="source" />,
    /// or <see cref="None" /> if the sequence is empty.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The last element of the task sequence, or None.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryLast: source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the last element of the input task sequence given by <paramref name="source" />.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The last element of the task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the task sequence is empty.</exception>
    static member last: source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the nth element of the input task sequence given by <paramref name="source" />,
    /// or <see cref="None" /> if the sequence does not contain enough elements.
    /// The index is zero-based, that is, using index 0 returns the first element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The nth element of the task sequence, or None if it doesn't exist.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryItem: index: int -> source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the nth element of the input task sequence given by <paramref name="source" />,
    /// or raises an exception if the sequence does not contain enough elements.
    /// The index is zero-based, that is, using index 0 returns the first element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The nth element of the task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the sequence has insufficient length or <paramref name="index" /> is negative.</exception>
    static member item: index: int -> source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the only element of the task sequence, or <see cref="None" /> if the sequence is empty of
    /// contains more than one element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The only element of the singleton task sequence, or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryExactlyOne: source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the only element of the task sequence.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The only element of the singleton task sequence, or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the input task sequence does not contain precisely one element.</exception>
    static member exactlyOne: source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to each element of the task sequence. Returns
    /// a sequence comprised of the results where the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is asynchronous, consider using <see cref="TaskSeq.chooseAsync" />.
    /// </summary>
    ///
    /// <param name="chooser">A function to transform items of type <typeref name="'T" /> into options of type <typeref name="'U" />.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member choose: chooser: ('T -> 'U option) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Applies the given asynchronous function <paramref name="chooser" /> to each element of the task sequence.
    /// Returns a sequence comprised of the results where the function returns a <see cref="task" /> result
    /// of <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is synchronous, consider using <see cref="TaskSeq.choose" />.
    /// </summary>
    ///
    /// <param name="chooser">An asynchronous function to transform items of type <typeref name="'T" /> into options of type <typeref name="'U" />.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member chooseAsync: chooser: ('T -> #Task<'U option>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Returns a new task sequence containing only the elements of the collection
    /// for which the given function <paramref name="predicate" /> returns <see cref="true" />.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.filterAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function to test whether an item in the input sequence should be included in the output or not.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member filter: predicate: ('T -> bool) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Returns a new task sequence containing only the elements of the input sequence
    /// for which the given function <paramref name="predicate" /> returns <see cref="true" />.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.filter" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function to test whether an item in the input sequence should be included in the output or not.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member filterAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, yields elements of the underlying sequence while the
    /// given function <paramref name="predicate" /> returns <see cref="true" />, and then returns no further elements.
    /// The first element where the predicate returns <see cref="false" /> is not included in the resulting sequence
    /// (see also <see cref="TaskSeq.takeWhileInclusive" />).
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.takeWhileAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function that evaluates to false when no more items should be returned.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member takeWhile: predicate: ('T -> bool) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Returns a sequence that, when iterated, yields elements of the underlying sequence while the
    /// given asynchronous function <paramref name="predicate" /> returns <see cref="true" />, and then returns no further elements.
    /// The first element where the predicate returns <see cref="false" /> is not included in the resulting sequence
    /// (see also <see cref="TaskSeq.takeWhileInclusive" />).
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.takeWhile" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to false when no more items should be returned.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member takeWhileAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Returns a sequence that, when iterated, yields elements of the underlying sequence until the given
    /// function <paramref name="predicate" /> returns <see cref="false" />, returns that element
    /// and then returns no further elements (see also <see cref="TaskSeq.takeWhile" />). This function returns
    /// at least one element of a non-empty sequence, or the empty task sequence if the input is empty.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.takeWhileInclusiveAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function that evaluates to false when no more items should be returned.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member takeWhileInclusive: predicate: ('T -> bool) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Returns a sequence that, when iterated, yields elements of the underlying sequence until the given
    /// asynchronous function <paramref name="predicate" /> returns <see cref="false" />, returns that element
    /// and then returns no further elements (see also <see cref="TaskSeq.takeWhile" />). This function returns
    /// at least one element of a non-empty sequence, or the empty task sequence if the input is empty.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.takeWhileInclusive" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to false when no more items should be returned.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member takeWhileInclusiveAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to successive elements, returning the first result where
    /// the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is asynchronous, consider using <see cref="TaskSeq.tryPickAsync" />.
    /// </summary>
    /// <param name="chooser">A function to transform items of type <typeref name="'T" /> into options of type <typeref name="'U" />.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The chosen element or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryPick: chooser: ('T -> 'U option) -> source: taskSeq<'T> -> Task<'U option>

    /// <summary>
    /// Applies the given asynchronous function <paramref name="chooser" /> to successive elements, returning the first result where
    /// the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is synchronous, consider using <see cref="TaskSeq.tryPick" />.
    /// </summary>
    /// <param name="chooser">An asynchronous function to transform items of type <typeref name="'T" /> into options of type <typeref name="'U" />.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The chosen element or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryPickAsync: chooser: ('T -> #Task<'U option>) -> source: taskSeq<'T> -> Task<'U option>

    /// <summary>
    /// Returns the first element for which the given function <paramref name="predicate" /> returns
    /// <see cref="true" />. Returns <see cref="None" /> if no such element exists.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.tryFindAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function that evaluates to a <see cref="bool" /> when given an item in the sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The found element or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryFind: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the first element for which the given asynchronous function <paramref name="predicate" /> returns
    /// <see cref="true" />. Returns <see cref="None" /> if no such element exists.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.tryFind" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to a <see cref="bool" /> when given an item in the sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The found element or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryFindAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the index, starting from zero, for which the given function <paramref name="predicate" /> returns
    /// <see cref="true" />. Returns <see cref="None" /> if no such element exists.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.tryFindIndexAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function that evaluates to a <see cref="bool" /> when given an item in the sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The found element or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryFindIndex: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<int option>

    /// <summary>
    /// Returns the index, starting from zero, for which the given asynchronous function <paramref name="predicate" /> returns
    /// <see cref="true" />. Returns <see cref="None" /> if no such element exists.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.tryFindIndex" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to a <see cref="bool" /> when given an item in the sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The found element or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryFindIndexAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<int option>

    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to successive elements, returning the first result where
    /// the function returns <see cref="Some(x)" />. Throws an exception if none is found.
    /// If <paramref name="chooser" /> is asynchronous, consider using <see cref="TaskSeq.pickAsync" />.
    /// </summary>
    ///
    /// <param name="chooser">A function to transform items of type <typeref name="'T" /> into options of type <typeref name="'U" />.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The selected element.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:KeyNotFoundException">Thrown when every item of the sequence evaluates to <see cref="None" /> when the given function is applied.</exception>
    static member pick: chooser: ('T -> 'U option) -> source: taskSeq<'T> -> Task<'U>

    /// <summary>
    /// Applies the given asynchronous function <paramref name="chooser" /> to successive elements, returning the first result where
    /// the function returns <see cref="Some(x)" />. Throws an exception if none is found.
    /// If <paramref name="chooser" /> is synchronous, consider using <see cref="TaskSeq.pick" />.
    /// </summary>
    ///
    /// <param name="chooser">An asynchronous function to transform items of type <typeref name="'T" /> into options of type <typeref name="'U" />.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The selected element.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:KeyNotFoundException">Thrown when every item of the sequence evaluates to <see cref="None" /> when the given function is applied.</exception>
    static member pickAsync: chooser: ('T -> #Task<'U option>) -> source: taskSeq<'T> -> Task<'U>

    /// <summary>
    /// Returns the first element for which the given function <paramref name="predicate" /> returns <see cref="true" />.
    /// Throws an exception if none is found.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.findAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function that evaluates to a <see cref="bool" /> when given an item in the sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The first element for which the predicate returns <see cref="true" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:KeyNotFoundException">Thrown if no element returns <see cref="true" /> when evaluated by the <paramref name="predicate" /> function.</exception>
    static member find: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the first element for which the given asynchronous function <paramref name="predicate" /> returns <see cref="true" />.
    /// Throws an exception if none is found.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.find" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to a <see cref="bool" /> when given an item in the sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The first element for which the predicate returns <see cref="true" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:KeyNotFoundException">Thrown if no element returns <see cref="true" /> when evaluated by the <paramref name="predicate" /> function.</exception>
    static member findAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the index, starting from zero, of the first element for which the given function <paramref name="predicate" />
    /// returns <see cref="true" />.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.findIndexAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function that evaluates to a <see cref="bool" /> when given an item in the sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The index for which the predicate returns <see cref="true" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:KeyNotFoundException">Thrown if no element returns <see cref="true" /> when evaluated by the <paramref name="predicate" /> function.</exception>
    static member findIndex: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the index, starting from zero, of the first element for which the given function <paramref name="predicate" />
    /// returns <see cref="true" />.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.findIndex" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to a <see cref="bool" /> when given an item in the sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The index for which the predicate returns <see cref="true" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:KeyNotFoundException">Thrown if no element returns <see cref="true" /> when evaluated by the <paramref name="predicate" /> function.</exception>
    static member findIndexAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Tests if the sequence contains the specified element. Returns <see cref="true" />
    /// if <paramref name="source" /> contains the specified element; <see cref="false" />
    /// otherwise. The input task sequence is only evaluated until the first element that matches the value.
    /// </summary>
    ///
    /// <param name="value">The value to locate in the input sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns><see cref="True" /> if the input sequence contains the specified element; <see cref="false" /> otherwise.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member contains<'T when 'T: equality> : value: 'T -> source: taskSeq<'T> -> Task<bool>

    /// <summary>
    /// Tests if any element of the task sequence in <paramref name="source" /> satisfies the given <paramref name="predicate" />. The function
    /// is applied to the elements of the input task sequence. If any application returns <see cref="true" /> then the overall result
    /// is <see cref="true" /> and no further elements are evaluated and tested.
    /// Otherwise, <see cref="false" /> is returned.
    /// </summary>
    ///
    /// <param name="predicate">A function to test each item of the input sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns><see cref="True" /> if any result from the predicate is true; <see cref="false" /> otherwise.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member exists: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<bool>

    /// <summary>
    /// Tests if any element of the task sequence in <paramref name="source" /> satisfies the given asynchronous <paramref name="predicate" />.
    /// The function is applied to the elements of the input task sequence. If any application returns <see cref="true" /> then the overall result
    /// is <see cref="true" /> and no further elements are evaluated and tested.
    /// Otherwise, <see cref="false" /> is returned.
    /// </summary>
    ///
    /// <param name="predicate">A function to test each item of the input sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns><see cref="True" /> if any result from the predicate is true; <see cref="false" /> otherwise.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member existsAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<bool>

    /// <summary>
    /// Returns a new task sequence with the distinct elements of the second task sequence which do not appear in the
    /// <paramref name="itemsToExclude" /> sequence, using generic hash and equality comparisons to compare values.
    /// </summary>
    ///
    /// <remarks>
    /// Note that this function returns a task sequence that digests the whole of the first input task sequence as soon as
    /// the resulting task sequence first gets awaited or iterated. As a result this function should not be used with
    /// large or infinite sequences in the first parameter. The function makes no assumption on the ordering of the first input
    /// sequence.
    /// </remarks>
    ///
    /// <param name="itemsToExclude">A task sequence whose elements that also occur in the second sequence will cause those elements to be removed from the returned sequence.</param>
    /// <param name="source">The input task sequence whose elements that are not also in the first will be returned.</param>
    /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
    ///
    /// <exception cref="T:ArgumentNullException">Thrown when either of the two input task sequences is null.</exception>
    static member except<'T when 'T: equality> : itemsToExclude: taskSeq<'T> -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Returns a new task sequence with the distinct elements of the second task sequence which do not appear in the
    /// <paramref name="itemsToExclude" /> sequence, using generic hash and equality comparisons to compare values.
    /// </summary>
    ///
    /// <remarks>
    /// Note that this function returns a task sequence that digests the whole of the first input task sequence as soon as
    /// the result sequence first gets awaited or iterated. As a result this function should not be used with
    /// large or infinite sequences in the first parameter. The function makes no assumption on the ordering of the first input
    /// sequence.
    /// </remarks>
    ///
    /// <param name="itemsToExclude">A task sequence whose elements that also occur in the second sequence will cause those elements to be removed from the returned sequence.</param>
    /// <param name="source">The input task sequence whose elements that are not also in first will be returned.</param>
    /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
    ///
    /// <exception cref="T:ArgumentNullException">Thrown when either of the two input task sequences is null.</exception>
    static member exceptOfSeq<'T when 'T: equality> : itemsToExclude: seq<'T> -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Combines the two task sequences into a new task sequence of pairs. The two sequences need not have equal lengths:
    /// when one sequence is exhausted any remaining elements in the other sequence are ignored.
    /// </summary>
    ///
    /// <param name="source1">The first input task sequence.</param>
    /// <param name="source2">The second input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the two input task sequences is null.</exception>
    static member zip: source1: taskSeq<'T> -> source2: taskSeq<'U> -> taskSeq<'T * 'U>

    /// <summary>
    /// Applies the function <paramref name="folder" /> to each element in the task sequence, threading an accumulator
    /// argument of type <typeref name="'State" /> through the computation.  If the input function is <paramref name="f" /> and the elements are <paramref name="i0...iN" />
    /// then computes <paramref name="f (... (f s i0)...) iN" />.
    /// If the accumulator function <paramref name="folder" /> is asynchronous, consider using <see cref="TaskSeq.foldAsync" />.
    /// </summary>
    ///
    /// <param name="folder">A function that updates the state with each element from the sequence.</param>
    /// <param name="state">The initial state.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The state object after the folding function is applied to each element of the sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member fold: folder: ('State -> 'T -> 'State) -> state: 'State -> source: taskSeq<'T> -> Task<'State>

    /// <summary>
    /// Applies the asynchronous function <paramref name="folder" /> to each element in the task sequence, threading an accumulator
    /// argument of type <typeref name="'State" /> through the computation.  If the input function is <paramref name="f" /> and the elements are <paramref name="i0...iN" />
    /// then computes <paramref name="f (... (f s i0)...) iN" />.
    /// If the accumulator function <paramref name="folder" /> is synchronous, consider using <see cref="TaskSeq.fold" />.
    /// </summary>
    ///
    /// <param name="folder">A function that updates the state with each element from the sequence.</param>
    /// <param name="state">The initial state.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The state object after the folding function is applied to each element of the sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member foldAsync:
        folder: ('State -> 'T -> #Task<'State>) -> state: 'State -> source: taskSeq<'T> -> Task<'State>
