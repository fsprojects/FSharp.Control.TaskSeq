namespace FSharp.Control

#nowarn "1204"

module TaskSeq =
    open System.Collections.Generic
    open System.Threading.Tasks

    /// Initialize an empty taskSeq.
    val empty<'T> : taskSeq<'T>

    /// <summary>
    /// Creates a <see cref="taskSeq" /> sequence from <paramref name="source" /> that generates a single element and then ends.
    /// </summary>
    ///
    /// <param name="value">The input item to use as the single value for the task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val singleton: value: 'T -> taskSeq<'T>

    /// <summary>
    /// Returns <see cref="true" /> if the task sequence contains no elements, <see cref="false" /> otherwise.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val isEmpty: source: taskSeq<'T> -> Task<bool>

    /// <summary>
    /// Returns the length of the sequence. This operation requires the whole sequence to be evaluated and
    /// should not be used on potentially infinite sequences, see <see cref="lengthOrMax" /> for an alternative.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val length: source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence, or <paramref name="max" />, whichever comes first. This operation requires the task sequence
    /// to be evaluated in full, or until <paramref name="max" /> items have been processed. Use this method instead of
    /// <see cref="TaskSeq.length" /> if you want to prevent too many items to be evaluated, or if the sequence is potentially infinite.
    /// </summary>
    ///
    /// <param name="max">The maximum value to return and the maximum items to count.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val lengthOrMax: max: int -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence of all items for which the <paramref name="predicate" /> returns true.
    /// This operation requires the whole sequence to be evaluated and should not be used on potentially infinite sequences.
    /// </summary>
    ///
    /// <param name="predicate">A function to test whether an item in the input sequence should be included in the count.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val lengthBy: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence of all items for which the <paramref name="predicate" /> returns true.
    /// This operation requires the whole sequence to be evaluated and should not be used on potentially infinite sequences.
    /// If <paramref name="predicate" /> does not need to be asynchronous, consider using <see cref="TaskSeq.lengthBy" />.
    /// </summary>
    ///
    /// <param name="predicate">A function to test whether an item in the input sequence should be included in the count.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val lengthByAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns a task sequence that is given by the delayed specification of a task sequence.
    /// </summary>
    ///
    /// <param name="generator">The generating function for the task sequence.</param>
    /// <returns>The generated task sequence.</returns>
    val delay: generator: (unit -> taskSeq<'T>) -> taskSeq<'T>

    /// <summary>
    /// Generates a new task sequence which, when iterated, will return successive elements by calling the given function
    /// with the current index, up to the given count. Each element is saved after its initialization for successive access to
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
    val init: count: int -> initializer: (int -> 'T) -> taskSeq<'T>

    /// <summary>
    /// Generates a new task sequence which, when iterated, will return successive elements by calling the given function
    /// with the current index, up to the given count. Each element is saved after its initialization for successive access to
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
    val initAsync: count: int -> initializer: (int -> #Task<'T>) -> taskSeq<'T>

    /// <summary>
    /// Generates a new task sequence which, when iterated, will return successive elements by calling the given function
    /// with the current index, ad infinitum, or until <see cref="Int32.MaxValue" /> is reached.
    /// Each element is saved after its initialization for successive access to
    /// <see cref="IAsyncEnumerator.Current" />, which will not re-evaluate the <paramref name="initializer" />. However,
    /// re-iterating the returned task sequence will re-evaluate the initialization function. The returned sequence may
    /// be passed between threads safely. However, individual IEnumerator values generated from the returned sequence should
    /// not be accessed concurrently.
    /// </summary>
    ///
    /// <param name="initializer">A function that generates an item in the sequence from a given index.</param>
    /// <returns>The resulting task sequence.</returns>
    val initInfinite: initializer: (int -> 'T) -> taskSeq<'T>

    /// <summary>
    /// Generates a new task sequence which, when iterated, will return successive elements by calling the given function
    /// with the current index, ad infinitum, or until <see cref="Int32.MaxValue" /> is reached.
    /// Each element is saved after its initialization for successive access to
    /// <see cref="IAsyncEnumerator.Current" />, which will not re-evaluate the <paramref name="initializer" />. However,
    /// re-iterating the returned task sequence will re-evaluate the initialization function. The returned sequence may
    /// be passed between threads safely. However, individual IEnumerator values generated from the returned sequence should
    /// not be accessed concurrently.
    /// </summary>
    ///
    /// <param name="initializer">A function that generates an item in the sequence from a given index.</param>
    /// <returns>The resulting task sequence.</returns>
    val initInfiniteAsync: initializer: (int -> #Task<'T>) -> taskSeq<'T>

    /// <summary>
    /// Combines the given task sequence of task sequences and concatenates them end-to-end, to form a
    /// new flattened, single task sequence. Each task sequence is awaited item by item, before the next is iterated.
    /// </summary>
    ///
    /// <param name="sources">The input task-sequence-of-task-sequences.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val concat: sources: taskSeq<#taskSeq<'T>> -> taskSeq<'T>

    /// <summary>
    /// Concatenates task sequences <paramref name="source1" /> and <paramref name="source2" /> in order as a single
    /// task sequence.
    /// </summary>
    ///
    /// <param name="source1">The first input task sequence.</param>
    /// <param name="source2">The second input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input sequences is null.</exception>
    val append: source1: taskSeq<'T> -> source2: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Concatenates a task sequence <paramref name="source1" /> with a non-async F# <see cref="seq" /> in <paramref name="source2" />
    /// and returns a single task sequence.
    /// </summary>
    ///
    /// <param name="source1">The input task sequence.</param>
    /// <param name="source2">The input F# <see cref="seq" /> sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input sequences is null.</exception>
    val appendSeq: source1: taskSeq<'T> -> source2: seq<'T> -> taskSeq<'T>

    /// <summary>
    /// Concatenates a non-async F# <see cref="seq" /> in <paramref name="source1" /> with a task sequence in <paramref name="source2" />
    /// and returns a single task sequence.
    /// </summary>
    ///
    /// <param name="source1">The input F# <see cref="seq" /> sequence.</param>
    /// <param name="source2">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input sequences is null.</exception>
    val prependSeq: source1: seq<'T> -> source2: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Builds an F# <see cref="list" /> from the input task sequence in <paramref name="source" />.
    /// This function is blocking until the sequence is exhausted and will then properly dispose of the resources.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting list.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val toList: source: taskSeq<'T> -> 'T list

    /// <summary>
    /// Builds an <see cref="array" /> from the input task sequence in <paramref name="source" />.
    /// This function is blocking until the sequence is exhausted and will then properly dispose of the resources.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting array.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val toArray: source: taskSeq<'T> -> 'T[]

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
    val toSeq: source: taskSeq<'T> -> seq<'T>

    /// <summary>
    /// Builds an <see cref="array" /> asynchronously from the input task sequence in <paramref name="source" />.
    /// This function is non-blocking while it builds the array.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting array.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val toArrayAsync: source: taskSeq<'T> -> Task<'T[]>

    /// <summary>
    /// Builds an F# <see cref="list" /> asynchronously from the input task sequence in <paramref name="source" />.
    /// This function is non-blocking while it builds the list.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting list.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val toListAsync: source: taskSeq<'T> -> Task<'T list>

    /// <summary>
    /// Builds a resizable array asynchronously from the input task sequence in <paramref name="source" />.
    /// This function is non-blocking while it builds the resizable array.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting resizable array.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val toResizeArrayAsync: source: taskSeq<'T> -> Task<ResizeArray<'T>>

    /// <summary>
    /// Builds an <see cref="IList&lt;'T>" /> asynchronously from the input task sequence in <paramref name="source" />.
    /// This function is non-blocking while it builds the IList.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting IList interface.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val toIListAsync: source: taskSeq<'T> -> Task<IList<'T>>

    /// <summary>
    /// Views the given <see cref="array" /> as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input array.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input array is null.</exception>
    val ofArray: source: 'T[] -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="list" /> as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input list.</param>
    /// <returns>The resulting task sequence.</returns>
    val ofList: source: 'T list -> taskSeq<'T>

    /// <summary>
    /// Views the given <see cref="seq" /> as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val ofSeq: source: seq<'T> -> taskSeq<'T>

    /// <summary>
    /// Views the given resizable array as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input resize array.</param>
    /// <returns>The resulting task sequence.</returns>
    val ofResizeArray: source: ResizeArray<'T> -> taskSeq<'T>

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
    val ofTaskSeq: source: seq<#Task<'T>> -> taskSeq<'T>

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
    val ofTaskList: source: #Task<'T> list -> taskSeq<'T>

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
    val ofTaskArray: source: #Task<'T> array -> taskSeq<'T>

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
    val ofAsyncSeq: source: seq<Async<'T>> -> taskSeq<'T>

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
    val ofAsyncList: source: Async<'T> list -> taskSeq<'T>

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
    val ofAsyncArray: source: Async<'T> array -> taskSeq<'T>

    /// <summary>
    /// Views each item in the input task sequence as <see cref="obj" />, boxing value types.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val box: source: taskSeq<'T> -> taskSeq<obj>

    /// <summary>
    /// Unboxes to the target type <see cref="'U" /> each item in the input task sequence.
    /// The target type must be a <see cref="struct" /> or a built-in value type.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    /// <exception cref="InvalidCastException">Thrown when the function is unable to cast an item to the target type.</exception>
    val unbox<'U when 'U: struct> : source: taskSeq<obj> -> taskSeq<'U>

    /// <summary>
    /// Casts each item in the untyped input task sequence. If the input sequence contains value types
    /// it is recommended to use <see cref="TaskSeq.unbox" /> instead.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    /// <exception cref="InvalidCastException">Thrown when the function is unable to cast an item to the target type.</exception>
    val cast: source: taskSeq<obj> -> taskSeq<'U>

    /// <summary>
    /// Iterates over the input task sequence, applying the <paramref name="action" /> function to each item.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">A function to apply to each element of the task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val iter: action: ('T -> unit) -> source: taskSeq<'T> -> Task<unit>

    /// <summary>
    /// Iterates over the input task sequence, applying the <paramref name="action" /> function to each item,
    /// carrying the index as extra parameter for the <paramref name="action" /> function.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">A function to apply to each element of the task sequence that can also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val iteri: action: (int -> 'T -> unit) -> source: taskSeq<'T> -> Task<unit>

    /// <summary>
    /// Iterates over the input task sequence, applying the asynchronous <paramref name="action" /> function to each item.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">An asynchronous function to apply to each element of the task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val iterAsync: action: ('T -> #Task<unit>) -> source: taskSeq<'T> -> Task<unit>

    /// <summary>
    /// Iterates over the input task sequence, applying the asynchronous <paramref name="action" /> function to each item,
    /// carrying the index as extra parameter for the <paramref name="action" /> function.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">An asynchronous function to apply to each element of the task sequence that can also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val iteriAsync: action: (int -> 'T -> #Task<unit>) -> source: taskSeq<'T> -> Task<unit>

    /// <summary>
    /// Builds a new task sequence whose elements are the corresponding elements of the input task
    /// sequence <paramref name="source" /> paired with the integer index (from 0) of each element.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence of tuples.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val indexed: source: taskSeq<'T> -> taskSeq<int * 'T>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="action" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />.
    /// The given function will be applied as elements are demanded using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapping">A function to transform items from the input task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val map: mapper: ('T -> 'U) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="action" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, passing
    /// an extra index argument to the <paramref name="action" /> function.
    /// The given function will be applied as elements are demanded using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapping">A function to transform items from the input task sequence that also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val mapi: mapper: (int -> 'T -> 'U) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="action" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />.
    /// The given function will be applied as elements are demanded using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapping">An asynchronous function to transform items from the input task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val mapAsync: mapper: ('T -> #Task<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="action" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, passing
    /// an extra index argument to the <paramref name="action" /> function.
    /// The given function will be applied as elements are demanded using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapping">An asynchronous function to transform items from the input task sequence that also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val mapiAsync: mapper: (int -> 'T -> #Task<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned task sequences.
    /// The given function will be applied as elements are demanded using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="binder">A function to transform items from the input task sequence into a task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val collect: binder: ('T -> #taskSeq<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned regular F# sequences.
    /// The given function will be applied as elements are demanded using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="binder">A function to transform items from the input task sequence into a regular sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val collectSeq: binder: ('T -> #seq<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned task sequences.
    /// The given function will be applied as elements are demanded using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="binder">An asynchronous function to transform items from the input task sequence into a task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val collectAsync: binder: ('T -> #Task<'TSeqU>) -> source: taskSeq<'T> -> taskSeq<'U> when 'TSeqU :> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned regular F# sequences.
    /// The given function will be applied as elements are demanded using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="binder">An asynchronous function to transform items from the input task sequence into a regular sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val collectSeqAsync: binder: ('T -> #Task<'SeqU>) -> source: taskSeq<'T> -> taskSeq<'U> when 'SeqU :> seq<'U>

    /// <summary>
    /// Returns the first element of the input task sequence given by <paramref name="source" />,
    /// or <see cref="None" /> if the sequence is empty.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The first element of the task sequence, or None.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val tryHead: source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the first elementof the input task sequence given by <paramref name="source" />.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The first element of the task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the sequence is empty.</exception>
    val head: source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the whole input task sequence given by <paramref name="source" />, minus its first element,
    /// or <see cref="None" /> if the sequence is empty.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The input task sequence minus the first element, or None.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val tryTail: source: taskSeq<'T> -> Task<taskSeq<'T> option>

    /// <summary>
    /// Returns the whole task sequence from <paramref name="source" />, minus its first element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The input task sequence minus the first element.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the sequence is empty.</exception>
    val tail: source: taskSeq<'T> -> Task<taskSeq<'T>>

    /// <summary>
    /// Returns the last element of the input task sequence given by <paramref name="source" />,
    /// or <see cref="None" /> if the sequence is empty.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The last element of the task sequence, or None.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val tryLast: source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the last element of the input task sequence given by <paramref name="source" />.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The last element of the task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the sequence is empty.</exception>
    val last: source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the nth element of the input task sequence given by <paramref name="source" />,
    /// or <see cref="None" /> if the sequence does not contain enough elements, or <paramref name="index" />
    /// is negative.
    /// The index is zero-based, that is, using index 0 returns the first element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The nth element of the task sequence, or None if it doesn't exist.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val tryItem: index: int -> source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the nth element of the input task sequence given by <paramref name="source" />,
    /// or raises an exception if the sequence does not contain enough elements, or <paramref name="index" />
    /// is negative.
    /// The index is zero-based, that is, using index 0 returns the first element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The nth element of the task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">
    ///    Thrown when the sequence has insufficient length or
    ///    <paramref name="index" /> is negative.
    /// </exception>
    val item: index: int -> source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the only element of the task sequence, or <see cref="None" /> if the sequence is empty of
    /// contains more than one element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The only element of the singleton task sequence, or None.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    val tryExactlyOne: source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the only element of the task sequence.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The only element of the singleton task sequence, or None.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the input sequence does not contain precisely one element.</exception>
    val exactlyOne: source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to each element of the task sequence. Returns
    /// a sequence comprised of the results "x" for each element where
    /// the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is asynchronous, consider using <see cref="TaskSeq.chooseAsync" />.
    /// </summary>
    val choose: chooser: ('T -> 'U option) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Applies the given asynchronous function <paramref name="chooser" /> to each element of the task sequence. Returns
    /// a sequence comprised of the results "x" for each element where
    /// the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> does not need to be asynchronous, consider using <see cref="TaskSeq.choose" />.
    /// </summary>
    val chooseAsync: chooser: ('T -> #Task<'U option>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Returns a new collection containing only the elements of the collection
    /// for which the given <paramref name="predicate" /> function returns <see cref="true" />.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.filterAsync" />.
    /// </summary>
    val filter: predicate: ('T -> bool) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Yields items from the source while the <paramref name="predicate" /> function returns <see cref="true" />.
    /// The first <see cref="false" /> result concludes consumption of the source.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.takeWhileAsync" />.
    /// </summary>
    val takeWhile: predicate: ('T -> bool) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Yields items from the source while the <paramref name="predicate" /> asynchronous function returns <see cref="true" />.
    /// The first <see cref="false" /> result concludes consumption of the source.
    /// If <paramref name="predicate" /> does not need to be asynchronous, consider using <see cref="TaskSeq.takeWhile" />.
    /// </summary>
    val takeWhileAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Yields items from the source while the <paramref name="predicate" /> function returns <see cref="true" />.
    /// The first <see cref="false" /> result concludes consumption of the source, but is included in the result.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.takeWhileInclusiveAsync" />.
    /// If the final item is not desired, consider using <see cref="TaskSeq.takeWhile" />.
    /// </summary>
    val takeWhileInclusive: predicate: ('T -> bool) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Yields items from the source while the <paramref name="predicate" /> asynchronous function returns <see cref="true" />.
    /// The first <see cref="false" /> result concludes consumption of the source, but is included in the result.
    /// If <paramref name="predicate" /> does not need to be asynchronous, consider using <see cref="TaskSeq.takeWhileInclusive" />.
    /// If the final item is not desired, consider using <see cref="TaskSeq.takeWhileAsync" />.
    /// </summary>
    val takeWhileInclusiveAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Returns a new collection containing only the elements of the collection
    /// for which the given asynchronous function <paramref name="predicate" /> returns <see cref="true" />.
    /// If <paramref name="predicate" /> does not need to be asynchronous, consider using <see cref="TaskSeq.filter" />.
    /// </summary>
    val filterAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to successive elements of the task sequence
    /// in <paramref name="source" />, returning the first result where the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is asynchronous, consider using <see cref="TaskSeq.tryPickAsync" />.
    /// </summary>
    val tryPick: chooser: ('T -> 'U option) -> source: taskSeq<'T> -> Task<'U option>

    /// <summary>
    /// Applies the given asynchronous function <paramref name="chooser" /> to successive elements of the task sequence
    /// in <paramref name="source" />, returning the first result where the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> does not need to be asynchronous, consider using <see cref="TaskSeq.tryPick" />.
    /// </summary>
    val tryPickAsync: chooser: ('T -> #Task<'U option>) -> source: taskSeq<'T> -> Task<'U option>

    /// <summary>
    /// Returns the first element of the task sequence in <paramref name="source" /> for which the given function
    /// <paramref name="predicate" /> returns <see cref="true" />. Returns <see cref="None" /> if no such element exists.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.tryFindAsync" />.
    /// </summary>
    val tryFind: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the first element of the task sequence in <paramref name="source" /> for which the given asynchronous function
    /// <paramref name="predicate" /> returns <see cref="true" />. Returns <see cref="None" /> if no such element exists.
    /// If <paramref name="predicate" /> does not need to be asynchronous, consider using <see cref="TaskSeq.tryFind" />.
    /// </summary>
    val tryFindAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the index, starting from zero, of the task sequence in <paramref name="source" /> for which the given function
    /// <paramref name="predicate" /> returns <see cref="true" />. Returns <see cref="None" /> if no such element exists.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.tryFindIndexAsync" />.
    /// </summary>
    val tryFindIndex: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<int option>

    /// <summary>
    /// Returns the index, starting from zero, of the task sequence in <paramref name="source" /> for which the given asynchronous function
    /// <paramref name="predicate" /> returns <see cref="true" />. Returns <see cref="None" /> if no such element exists.
    /// If <paramref name="predicate" /> does not need to be asynchronous, consider using <see cref="TaskSeq.tryFindIndex" />.
    /// </summary>
    val tryFindIndexAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<int option>


    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to successive elements of the task sequence
    /// in <paramref name="source" />, returning the first result where the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is asynchronous, consider using <see cref="TaskSeq.pickAsync" />.
    /// <exception cref="KeyNotFoundException">Thrown when every item of the sequence
    /// evaluates to <see cref="None" /> when the given function is applied.</exception>
    /// </summary>
    val pick: chooser: ('T -> 'U option) -> source: taskSeq<'T> -> Task<'U>

    /// <summary>
    /// Applies the given asynchronous function <paramref name="chooser" /> to successive elements of the task sequence
    /// in <paramref name="source" />, returning the first result where the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> does not need to be asynchronous, consider using <see cref="TaskSeq.pick" />.
    /// <exception cref="KeyNotFoundException">Thrown when every item of the sequence
    /// evaluates to <see cref="None" /> when the given function is applied.</exception>
    /// </summary>
    val pickAsync: chooser: ('T -> #Task<'U option>) -> source: taskSeq<'T> -> Task<'U>

    /// <summary>
    /// Returns the first element of the task sequence in <paramref name="source" /> for which the given function
    /// <paramref name="predicate" /> returns <see cref="true" />.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.findAsync" />.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if no element returns <see cref="true" /> when
    /// evaluated by the <paramref name="predicate" /> function.</exception>
    val find: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the first element of the task sequence in <paramref name="source" /> for which the given
    /// asynchronous function <paramref name="predicate" /> returns <see cref="true" />.
    /// If <paramref name="predicate" /> does not need to be asynchronous, consider using <see cref="TaskSeq.find" />.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if no element returns <see cref="true" /> when
    /// evaluated by the <paramref name="predicate" /> function.</exception>
    val findAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the index, starting from zero, of the first element of the task sequence in <paramref name="source" /> for which
    /// the given function <paramref name="predicate" /> returns <see cref="true" />.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.findIndexAsync" />.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if no element returns <see cref="true" /> when
    /// evaluated by the <paramref name="predicate" /> function.</exception>
    val findIndex: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the index, starting from zero, of the task sequence in <paramref name="source" /> for which the given
    /// asynchronous function <paramref name="predicate" /> returns <see cref="true" />.
    /// If <paramref name="predicate" /> does not need to be asynchronous, consider using <see cref="TaskSeq.findIndex" />.
    /// </summary>
    ///
    /// <exception cref="KeyNotFoundException">Thrown if no element returns <see cref="true" /> when
    /// evaluated by the <paramref name="predicate" /> function.</exception>
    val findIndexAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Tests if the sequence contains the specified element. Returns <see cref="true" />
    /// if <paramref name="source" /> contains the specified element; <see cref="false" />
    /// otherwise.
    /// </summary>
    ///
    /// <param name="value">The value to locate in the input sequence.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>True if the input sequence contains the specified element; false otherwise.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val contains<'T when 'T: equality> : value: 'T -> source: taskSeq<'T> -> Task<bool>

    /// <summary>
    /// Tests if any element of the task sequence in <paramref name="source" /> satisfies
    /// the given <paramref name="predicate" />.
    /// The <paramref name="predicate" /> function is applied to the elements of the input sequence. If any application
    /// returns <see cref="true" /> then the overall result is <see cref="true" /> and no further elements are evaluated and tested.
    /// Otherwise, <see cref="false" /> is returned.
    /// </summary>
    ///
    /// <param name="predicate">A function to test each item of the input sequence.</param>
    /// <param name="source">The input sequence.</param>    ///
    /// <returns>True if any result from the predicate is true; false otherwise.</returns>    ///
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val exists: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<bool>

    /// <summary>
    /// Tests if any element of the task sequence in <paramref name="source" /> satisfies
    /// the given async <paramref name="predicate" />.
    /// The <paramref name="predicate" /> function is applied to the elements of the input sequence. If any application
    /// returns <see cref="true" /> then the overall result is <see cref="true" /> and no further elements are evaluated and tested.
    /// Otherwise, <see cref="false" /> is returned.
    /// </summary>
    ///
    /// <param name="predicate">A function to test each item of the input sequence.</param>
    /// <param name="source">The input sequence.</param>    ///
    /// <returns>True if any result from the predicate is true; false otherwise.</returns>    ///
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val existsAsync: predicate: ('T -> #Task<bool>) -> source: taskSeq<'T> -> Task<bool>

    /// <summary>
    /// Returns a new task sequence with the distinct elements of the second task sequence which do not appear in the
    /// <paramref name="itemsToExclude" />, using generic hash and equality comparisons to compare values.
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
    /// <param name="source">A sequence whose elements that are not also in first will be returned.</param>
    /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
    ///
    /// <exception cref="T:ArgumentNullException">Thrown when either of the two input sequences is null.</exception>
    val except<'T when 'T: equality> : itemsToExclude: taskSeq<'T> -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Returns a new task sequence with the distinct elements of the second task sequence which do not appear in the
    /// <paramref name="itemsToExclude" />, using generic hash and equality comparisons to compare values.
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
    /// <param name="source">A sequence whose elements that are not also in first will be returned.</param>
    /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
    ///
    /// <exception cref="T:ArgumentNullException">Thrown when either of the two input sequences is null.</exception>
    val exceptOfSeq<'T when 'T: equality> : itemsToExclude: seq<'T> -> source: taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Zips two task sequences, returning a taskSeq of the tuples of each sequence, in order. May raise ArgumentException
    /// if the sequences are or unequal length.
    /// </summary>
    /// <exception cref="ArgumentException">The sequences have different lengths.</exception>
    val zip: source1: taskSeq<'T> -> source2: taskSeq<'U> -> taskSeq<'T * 'U>

    /// <summary>
    /// Applies the function <paramref name="folder" /> to each element in the task sequence,
    /// threading an accumulator argument of type <paramref name="'State" /> through the computation.
    /// If the accumulator function <paramref name="folder" /> is asynchronous, consider using <see cref="TaskSeq.foldAsync" />.
    /// </summary>
    val fold: folder: ('State -> 'T -> 'State) -> state: 'State -> source: taskSeq<'T> -> Task<'State>

    /// <summary>
    /// Applies the asynchronous function <paramref name="folder" /> to each element in the task sequence,
    /// threading an accumulator argument of type <paramref name="'State" /> through the computation.
    /// If the accumulator function <paramref name="folder" /> does not need to be asynchronous, consider using <see cref="TaskSeq.fold" />.
    /// </summary>
    val foldAsync: folder: ('State -> 'T -> #Task<'State>) -> state: 'State -> source: taskSeq<'T> -> Task<'State>
