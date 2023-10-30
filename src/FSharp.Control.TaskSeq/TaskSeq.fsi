namespace FSharp.Control

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

[<AutoOpen>]
module TaskSeqExtensions =
    module TaskSeq =
        /// Initialize an empty task sequence.
        val empty<'T> : TaskSeq<'T>

[<Sealed; AbstractClass>]
type TaskSeq =

    /// <summary>
    /// Creates a task sequence from <paramref name="value" /> that generates a single element and then ends.
    /// </summary>
    ///
    /// <param name="value">The input item to use as the single item of the task sequence.</param>
    static member singleton: value: 'T -> TaskSeq<'T>

    /// <summary>
    /// Returns <see cref="true" /> if the task sequence contains no elements, <see cref="false" /> otherwise.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member isEmpty: source: TaskSeq<'T> -> Task<bool>


    /// <summary>
    /// Returns the <see cref="isEmpty" /> function with the given <see cref="CancellationToken" /> in its closure, which
    /// returns <see cref="true" /> is the cancelaable task sequence contains no elements, <see cref="false" /> otherwise.
    /// </summary>
    ///
    /// <param name="token">The cancellation token.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence for the returned function is null.</exception>
    static member isEmpty: token: CancellationToken -> (TaskSeq<'T> -> Task<bool>)

    /// <summary>
    /// Returns the length of the sequence. This operation requires the whole sequence to be evaluated and
    /// should not be used on potentially infinite sequences, see <see cref="TaskSeq.lengthOrMax" /> for an alternative.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member length: source: TaskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence, or <paramref name="max" />, whichever comes first. This operation requires the task sequence
    /// to be evaluated ether in full, or until <paramref name="max" /> items have been processed. Use this method instead of
    /// <see cref="TaskSeq.length" /> if you need to limit the number of items evaluated, or if the sequence is potentially infinite.
    /// </summary>
    ///
    /// <param name="max">Limit at which to stop evaluating source items for finding the length.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member lengthOrMax: max: int -> source: TaskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence of all items for which the <paramref name="predicate" /> returns true.
    /// This operation requires the whole sequence to be evaluated and should not be used on potentially infinite sequences.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.lengthByAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function to test whether an item in the input sequence should be included in the count.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member lengthBy: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence of all items for which the <paramref name="predicate" /> returns true.
    /// This operation requires the whole sequence to be evaluated and should not be used on potentially infinite sequences.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.lengthBy" />.
    /// </summary>
    ///
    /// <param name="predicate">A function to test whether an item in the input sequence should be included in the count.</param>
    /// <param name="source">The input task sequence.</param>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member lengthByAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the greatest of all elements of the sequence, compared via <see cref="Operators.max" />.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The largest element of the sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the input task sequence is empty.</exception>
    static member max: source: TaskSeq<'T> -> Task<'T> when 'T: comparison

    /// <summary>
    /// Returns the smallest of all elements of the sequence, compared via <see cref="Operators.min" />.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The smallest element of the sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the input task sequence is empty.</exception>
    static member min: source: TaskSeq<'T> -> Task<'T> when 'T: comparison

    /// <summary>
    /// Returns the greatest of all elements of the task sequence, compared via <see cref="Operators.max" />
    /// on the result of applying the function <paramref name="projection" /> to each element.
    ///
    /// If <paramref name="projection" /> is asynchronous, consider using <see cref="TaskSeq.maxByAsync" />.
    /// </summary>
    ///
    /// <param name="projection">A function to transform items from the input sequence into comparable keys.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The largest element of the sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the input sequence is empty.</exception>
    static member maxBy: projection: ('T -> 'U) -> source: TaskSeq<'T> -> Task<'T> when 'U: comparison

    /// <summary>
    /// Returns the smallest of all elements of the task sequence, compared via <see cref="Operators.min" />
    /// on the result of applying the function <paramref name="projection" /> to each element.
    ///
    /// If <paramref name="projection" /> is asynchronous, consider using <see cref="TaskSeq.minByAsync" />.
    /// </summary>
    ///
    /// <param name="projection">A function to transform items from the input sequence into comparable keys.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The smallest element of the sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the input sequence is empty.</exception>
    static member minBy: projection: ('T -> 'U) -> source: TaskSeq<'T> -> Task<'T> when 'U: comparison

    /// <summary>
    /// Returns the greatest of all elements of the task sequence, compared via <see cref="Operators.max" />
    /// on the result of applying the function <paramref name="projection" /> to each element.
    ///
    /// If <paramref name="projection" /> is synchronous, consider using <see cref="TaskSeq.maxBy" />.
    /// </summary>
    ///
    /// <param name="projection">A function to transform items from the input sequence into comparable keys.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The largest element of the sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the input sequence is empty.</exception>
    static member maxByAsync: projection: ('T -> #Task<'U>) -> source: TaskSeq<'T> -> Task<'T> when 'U: comparison

    /// <summary>
    /// Returns the smallest of all elements of the task sequence, compared via <see cref="Operators.min" />
    /// on the result of applying the function <paramref name="projection" /> to each element.
    ///
    /// If <paramref name="projection" /> is synchronous, consider using <see cref="TaskSeq.minBy" />.
    /// </summary>
    ///
    /// <param name="projection">A function to transform items from the input sequence into comparable keys.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The smallest element of the sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the input sequence is empty.</exception>
    static member minByAsync: projection: ('T -> #Task<'U>) -> source: TaskSeq<'T> -> Task<'T> when 'U: comparison

    /// <summary>
    /// Returns a task sequence that is given by the delayed specification of a task sequence.
    /// </summary>
    ///
    /// <param name="generator">The generating function for the task sequence.</param>
    /// <returns>The generated task sequence.</returns>
    static member delay: generator: (unit -> TaskSeq<'T>) -> TaskSeq<'T>

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
    static member init: count: int -> initializer: (int -> 'T) -> TaskSeq<'T>

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
    static member initAsync: count: int -> initializer: (int -> #Task<'T>) -> TaskSeq<'T>

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
    static member initInfinite: initializer: (int -> 'T) -> TaskSeq<'T>

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
    static member initInfiniteAsync: initializer: (int -> #Task<'T>) -> TaskSeq<'T>

    /// <summary>
    /// Combines the given task sequence of task sequences and concatenates them end-to-end, to form a
    /// new flattened, single task sequence, like <paramref name="TaskSeq.collect id"/>. Each task sequence is
    /// awaited and consumed in full, before the next one is iterated.
    /// </summary>
    ///
    /// <param name="sources">The input task-sequence-of-task-sequences.</param>
    /// <returns>The resulting, flattened task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence of task sequences is null.</exception>
    static member concat: sources: TaskSeq<#TaskSeq<'T>> -> TaskSeq<'T>

    /// <summary>
    /// Combines the given task sequence of sequences and concatenates them end-to-end, to form a
    /// new flattened, single task sequence.
    /// </summary>
    ///
    /// <param name="sources">The input task sequence of sequences.</param>
    /// <returns>The resulting, flattened task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence of task sequences is null.</exception>
    static member concat: sources: TaskSeq<'T seq> -> TaskSeq<'T>

    /// <summary>
    /// Combines the given task sequence of arrays and concatenates them end-to-end, to form a
    /// new flattened, single task sequence.
    /// </summary>
    ///
    /// <param name="sources">The input task sequence of arrays.</param>
    /// <returns>The resulting, flattened task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence of task sequences is null.</exception>
    static member concat: sources: TaskSeq<'T[]> -> TaskSeq<'T>

    /// <summary>
    /// Combines the given task sequence of lists and concatenates them end-to-end, to form a
    /// new flattened, single task sequence.
    /// </summary>
    ///
    /// <param name="sources">The input task sequence of lists.</param>
    /// <returns>The resulting, flattened task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence of task sequences is null.</exception>
    static member concat: sources: TaskSeq<'T list> -> TaskSeq<'T>

    /// <summary>
    /// Combines the given task sequence of resizable arrays and concatenates them end-to-end, to form a
    /// new flattened, single task sequence.
    /// </summary>
    ///
    /// <param name="sources">The input task sequence of resizable arrays.</param>
    /// <returns>The resulting, flattened task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence of task sequences is null.</exception>
    static member concat: sources: TaskSeq<ResizeArray<'T>> -> TaskSeq<'T>

    /// <summary>
    /// Concatenates task sequences <paramref name="source1" /> and <paramref name="source2" /> in order as a single
    /// task sequence.
    /// </summary>
    ///
    /// <param name="source1">The first input task sequence.</param>
    /// <param name="source2">The second input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input task sequences is null.</exception>
    static member append: source1: TaskSeq<'T> -> source2: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Concatenates a task sequence <paramref name="source1" /> with a non-async F# <see cref="seq" /> in <paramref name="source2" />
    /// and returns a single task sequence.
    /// </summary>
    ///
    /// <param name="source1">The input task sequence.</param>
    /// <param name="source2">The input F# <see cref="seq" /> sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input sequences is null.</exception>
    static member appendSeq: source1: TaskSeq<'T> -> source2: seq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Concatenates a non-async F# <see cref="seq" /> in <paramref name="source1" /> with a task sequence in <paramref name="source2" />
    /// and returns a single task sequence.
    /// </summary>
    ///
    /// <param name="source1">The input F# <see cref="seq" /> sequence.</param>
    /// <param name="source2">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input sequences is null.</exception>
    static member prependSeq: source1: seq<'T> -> source2: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Builds an F# <see cref="list" /> from the input task sequence in <paramref name="source" />.
    /// This function is blocking until the sequence is exhausted and will then properly dispose of the resources.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting list.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toList: source: TaskSeq<'T> -> 'T list

    /// <summary>
    /// Builds an <see cref="array" /> from the input task sequence in <paramref name="source" />.
    /// This function is blocking until the sequence is exhausted and will then properly dispose of the resources.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting array.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toArray: source: TaskSeq<'T> -> 'T[]

    /// <summary>
    /// Views the task sequence in <paramref name="source" /> as an F# <see cref="seq" />, that is, an
    /// <see cref="IEnumerable&lt;_>" />. This function is blocking at each call
    /// to <see cref="IEnumerator&lt;_>.MoveNext()" /> in the resulting sequence.
    /// Resources are disposed when the enumerator is disposed, or the enumerator is exhausted.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toSeq: source: TaskSeq<'T> -> seq<'T>

    /// <summary>
    /// Builds an <see cref="array" /> asynchronously from the input task sequence.
    /// This function is non-blocking while it builds the array.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting array.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toArrayAsync: source: TaskSeq<'T> -> Task<'T[]>

    /// <summary>
    /// Builds an F# <see cref="list" /> asynchronously from the input task sequence.
    /// This function is non-blocking while it builds the list.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting list.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toListAsync: source: TaskSeq<'T> -> Task<'T list>

    /// <summary>
    /// Gathers items into a ResizeArray (see <see cref="T:System.Collections.Generic.List&lt;_>" />) asynchronously from the input task sequence.
    /// This function is non-blocking while it builds the resizable array.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting resizable array.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toResizeArrayAsync: source: TaskSeq<'T> -> Task<ResizeArray<'T>>

    /// <summary>
    /// Builds an <see cref="IList&lt;'T>" /> asynchronously from the input task sequence.
    /// This function is non-blocking while it builds the IList.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting IList interface.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member toIListAsync: source: TaskSeq<'T> -> Task<IList<'T>>

    /// <summary>
    /// Views the given <see cref="array" /> as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input array.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input array is null.</exception>
    static member ofArray: source: 'T[] -> TaskSeq<'T>

    /// <summary>
    /// Views the given <see cref="list" /> as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input list.</param>
    /// <returns>The resulting task sequence.</returns>
    static member ofList: source: 'T list -> TaskSeq<'T>

    /// <summary>
    /// Views the given <see cref="seq" /> as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member ofSeq: source: seq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Views the given resizable array as a task sequence, that is, as an <see cref="IAsyncEnumerable&lt;'T>" />.
    /// </summary>
    ///
    /// <param name="source">The input resize array.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input resize array is null.</exception>
    static member ofResizeArray: source: ResizeArray<'T> -> TaskSeq<'T>

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
    static member ofTaskSeq: source: seq<#Task<'T>> -> TaskSeq<'T>

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
    static member ofTaskList: source: #Task<'T> list -> TaskSeq<'T>

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
    static member ofTaskArray: source: #Task<'T> array -> TaskSeq<'T>

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
    static member ofAsyncSeq: source: seq<Async<'T>> -> TaskSeq<'T>

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
    static member ofAsyncList: source: Async<'T> list -> TaskSeq<'T>

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
    static member ofAsyncArray: source: Async<'T> array -> TaskSeq<'T>

    /// <summary>
    /// Views each item in the input task sequence as <see cref="obj" />, boxing value types.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member box: source: TaskSeq<'T> -> TaskSeq<obj>

    /// <summary>
    /// Calls <see cref="unbox" /> on each item when the task sequence is consumed.
    /// The target type must be a value type.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="InvalidCastException">Thrown when the function is unable to cast an item to the target type.</exception>
    static member unbox<'U when 'U: struct> : source: TaskSeq<obj> -> TaskSeq<'U>

    /// <summary>
    /// Casts each item in the untyped input task sequence. If the input sequence contains value types
    /// it is recommended to consider using <see cref="TaskSeq.unbox" /> instead.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="InvalidCastException">Thrown when the function is unable to cast an item to the target type.</exception>
    static member cast: source: TaskSeq<obj> -> TaskSeq<'U>

    /// <summary>
    /// Iterates over the input task sequence, applying the <paramref name="action" /> function to each item.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">A function to apply to each element of the task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>A <see cref="unit" /> <see cref="task" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    static member iter: action: ('T -> unit) -> source: TaskSeq<'T> -> Task<unit>

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
    static member iteri: action: (int -> 'T -> unit) -> source: TaskSeq<'T> -> Task<unit>

    /// <summary>
    /// Iterates over the input task sequence, applying the asynchronous <paramref name="action" /> function to each item.
    /// This function is non-blocking, but will exhaust the full input sequence as soon as the task is evaluated.
    /// </summary>
    ///
    /// <param name="action">An asynchronous function to apply to each element of the task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>A <see cref="unit" /> <see cref="task" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member iterAsync: action: ('T -> #Task<unit>) -> source: TaskSeq<'T> -> Task<unit>

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
    static member iteriAsync: action: (int -> 'T -> #Task<unit>) -> source: TaskSeq<'T> -> Task<unit>

    /// <summary>
    /// Builds a new task sequence whose elements are the corresponding elements of the input task
    /// sequence <paramref name="source" /> paired with the integer index (from 0) of each element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence of tuples.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member indexed: source: TaskSeq<'T> -> TaskSeq<int * 'T>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="mapper" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />.
    /// The given function will be applied as elements are pulled using async enumerators retrieved from the
    /// input task sequence.
    ///
    /// If <paramref name="mapper" /> is asynchronous, consider using <see cref="TaskSeq.mapAsync" />.
    /// </summary>
    ///
    /// <param name="mapper">A function to transform items from the input task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member map: mapper: ('T -> 'U) -> source: TaskSeq<'T> -> TaskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="mapper" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, passing
    /// an extra zero-based index argument to the <paramref name="mapper" /> function.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapper">A function to transform items from the input task sequence that also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member mapi: mapper: (int -> 'T -> 'U) -> source: TaskSeq<'T> -> TaskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="mapper" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />.
    /// The given function will be applied as elements are pulled using async enumerators retrieved from the
    /// input task sequence.
    ///
    /// If <paramref name="mapper" /> is synchronous, consider using <see cref="TaskSeq.map" />.
    /// </summary>
    ///
    /// <param name="mapper">An asynchronous function to transform items from the input task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member mapAsync: mapper: ('T -> #Task<'U>) -> source: TaskSeq<'T> -> TaskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="mapper" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, passing
    /// an extra zero-based index argument to the <paramref name="mapper" /> function.
    /// The given function will be applied as elements are pulled using the <see cref="MoveNextAsync" />
    /// method on async enumerators retrieved from the input task sequence.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    ///
    /// <param name="mapper">An asynchronous function to transform items from the input task sequence that also access the current index.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member mapiAsync: mapper: (int -> 'T -> #Task<'U>) -> source: TaskSeq<'T> -> TaskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned task sequences.
    /// The given function will be applied as elements are pulled using async enumerators retrieved from the
    /// input task sequence.
    ///
    /// If <paramref name="binder" /> is asynchronous, consider using <see cref="TaskSeq.collectAsync" />.
    /// </summary>
    ///
    /// <param name="binder">A function to transform items from the input task sequence into a task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member collect: binder: ('T -> #TaskSeq<'U>) -> source: TaskSeq<'T> -> TaskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned regular F# sequences.
    /// The given function will be applied as elements are pulled using async enumerators retrieved from the
    /// input task sequence.
    ///
    /// If <paramref name="binder" /> is asynchronous, consider using <see cref="TaskSeq.collectSeqAsync" />.
    /// </summary>
    ///
    /// <param name="binder">A function to transform items from the input task sequence into a regular sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member collectSeq: binder: ('T -> #seq<'U>) -> source: TaskSeq<'T> -> TaskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned task sequences.
    /// The given function will be applied as elements are pulled using async enumerators retrieved from the
    /// input task sequence.
    ///
    /// If <paramref name="binder" /> is synchronous, consider using <see cref="TaskSeq.collect" />.
    /// </summary>
    ///
    /// <param name="binder">An asynchronous function to transform items from the input task sequence into a task sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member collectAsync:
        binder: ('T -> #Task<'TSeqU>) -> source: TaskSeq<'T> -> TaskSeq<'U> when 'TSeqU :> TaskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the results of applying the asynchronous <paramref name="binder" />
    /// function to each of the elements of the input task sequence in <paramref name="source" />, and concatenating the
    /// returned regular F# sequences.
    /// The given function will be applied as elements are pulled using async enumerators retrieved from the
    /// input task sequence.
    ///
    /// If <paramref name="binder" /> is synchronous, consider using <see cref="TaskSeq.collectSeqAsync" />.
    /// </summary>
    ///
    /// <param name="binder">An asynchronous function to transform items from the input task sequence into a regular sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting concatenation of all returned task sequences.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member collectSeqAsync:
        binder: ('T -> #Task<'SeqU>) -> source: TaskSeq<'T> -> TaskSeq<'U> when 'SeqU :> seq<'U>

    /// <summary>
    /// Returns the first element of the input task sequence given by <paramref name="source" />,
    /// or <see cref="None" /> if the sequence is empty.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The first element of the task sequence, or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryHead: source: TaskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the first element of the input task sequence given by <paramref name="source" />.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The first element of the task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the task sequence is empty.</exception>
    static member head: source: TaskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the whole input task sequence given by <paramref name="source" />, minus its first element,
    /// or <see cref="None" /> if the sequence is empty.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The input task sequence minus the first element, or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryTail: source: TaskSeq<'T> -> Task<TaskSeq<'T> option>

    /// <summary>
    /// Returns the whole task sequence from <paramref name="source" />, minus its first element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The input task sequence minus the first element.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the task sequence is empty.</exception>
    static member tail: source: TaskSeq<'T> -> Task<TaskSeq<'T>>

    /// <summary>
    /// Returns the last element of the input task sequence given by <paramref name="source" />,
    /// or <see cref="None" /> if the sequence is empty.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The last element of the task sequence, or None.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryLast: source: TaskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the last element of the input task sequence given by <paramref name="source" />.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The last element of the task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the task sequence is empty.</exception>
    static member last: source: TaskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the nth element of the input task sequence given by <paramref name="source" />,
    /// or <see cref="None" /> if the sequence does not contain enough elements.
    /// The index is zero-based, that is, using index 0 returns the first element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <param name="index">The index of the item to retrieve.</param>
    /// <returns>The nth element of the task sequence, or None if it doesn't exist.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryItem: index: int -> source: TaskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the nth element of the input task sequence given by <paramref name="source" />,
    /// or raises an exception if the sequence does not contain enough elements.
    /// The index is zero-based, that is, using index 0 returns the first element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <param name="index">The index of the item to retrieve.</param>
    /// <returns>The nth element of the task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the sequence has insufficient length or <paramref name="index" /> is negative.</exception>
    static member item: index: int -> source: TaskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the only element of the task sequence, or <see cref="None" /> if the sequence is empty of
    /// contains more than one element.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The only element of the singleton task sequence, or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryExactlyOne: source: TaskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the only element of the task sequence.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The only element of the singleton task sequence, or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when the input task sequence does not contain precisely one element.</exception>
    static member exactlyOne: source: TaskSeq<'T> -> Task<'T>

    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to each element of the task sequence. Returns
    /// a sequence comprised of the results where the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is asynchronous, consider using <see cref="TaskSeq.chooseAsync" />.
    /// </summary>
    ///
    /// <param name="chooser">A function to transform items of type <paramref name="'T" /> into options of type <paramref name="'U" />.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member choose: chooser: ('T -> 'U option) -> source: TaskSeq<'T> -> TaskSeq<'U>

    /// <summary>
    /// Applies the given asynchronous function <paramref name="chooser" /> to each element of the task sequence.
    /// Returns a sequence comprised of the results where the function returns a <see cref="task" /> result
    /// of <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is synchronous, consider using <see cref="TaskSeq.choose" />.
    /// </summary>
    ///
    /// <param name="chooser">An asynchronous function to transform items of type <paramref name="'T" /> into options of type <paramref name="'U" />.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member chooseAsync: chooser: ('T -> #Task<'U option>) -> source: TaskSeq<'T> -> TaskSeq<'U>

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
    static member filter: predicate: ('T -> bool) -> source: TaskSeq<'T> -> TaskSeq<'T>

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
    static member filterAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a new task sequence containing only the elements of the collection
    /// for which the given function <paramref name="predicate" /> returns <see cref="true" />.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.whereAsync" />.
    ///
    /// Alias for <see cref="TaskSeq.filter" />.
    /// </summary>
    ///
    /// <param name="predicate">A function to test whether an item in the input sequence should be included in the output or not.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member where: predicate: ('T -> bool) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a new task sequence containing only the elements of the input sequence
    /// for which the given function <paramref name="predicate" /> returns <see cref="true" />.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.where" />.
    ///
    /// Alias for <see cref="TaskSeq.filterAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function to test whether an item in the input sequence should be included in the output or not.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member whereAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Tests if all elements of the sequence satisfy the given predicate. Stops evaluating
    /// as soon as <paramref name="predicate" /> returns <see cref="false" />.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.forallAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function to test an element of the input sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>A task that, after awaiting, holds true if every element of the sequence satisfies the predicate; false otherwise.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member forall: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<bool>

    /// <summary>
    /// Tests if all elements of the sequence satisfy the given asynchronous predicate. Stops evaluating
    /// as soon as <paramref name="predicate" /> returns <see cref="false" />.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.forall" />.
    /// </summary>
    ///
    /// <param name="predicate">A function to test an element of the input sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>A task that, after awaiting, holds true if every element of the sequence satisfies the predicate; false otherwise.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member forallAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<bool>

    /// <summary>
    /// Returns a task sequence that, when iterated, skips <paramref name="count" /> elements of the underlying
    /// sequence, and then yields the remainder. Raises an exception if there are not <paramref name="count" />
    /// items. See <see cref="TaskSeq.drop" /> for a version that does not raise an exception.
    /// See also <see cref="TaskSeq.take" /> for the inverse of this operation.
    /// </summary>
    ///
    /// <param name="count">The number of items to skip.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">
    ///     Thrown when <paramref name="count" /> is less than zero or when
    ///     it exceeds the number of elements in the sequence.
    /// </exception>
    static member skip: count: int -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, drops at most <paramref name="count" /> elements of the
    /// underlying sequence, and then returns the remainder of the elements, if any.
    /// See <see cref="TaskSeq.skip" /> for a version that raises an exception if there
    /// are not enough elements. See also <see cref="TaskSeq.truncate" /> for the inverse of this operation.
    /// </summary>
    ///
    /// <param name="count">The maximum number of items to drop.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when <paramref name="count" /> is less than zero.</exception>
    static member drop: count: int -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, yields <paramref name="count" /> elements of the
    /// underlying sequence, and then returns no further elements. Raises an exception if there are not enough
    /// elements in the sequence. See <see cref="TaskSeq.truncate" /> for a version that does not raise an exception.
    /// See also <see cref="TaskSeq.skip" /> for the inverse of this operation.
    /// </summary>
    ///
    /// <param name="count">The number of items to take.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">
    ///     Thrown when <paramref name="count" /> is less than zero or when
    ///     it exceeds the number of elements in the sequence.
    /// </exception>
    static member take: count: int -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, yields at most <paramref name="count" /> elements of the underlying
    /// sequence, truncating the remainder, if any.
    /// See <see cref="TaskSeq.take" /> for a version that raises an exception if there are not enough elements in the
    /// sequence. See also <see cref="TaskSeq.drop" /> for the inverse of this operation.
    /// </summary>
    ///
    /// <param name="count">The maximum number of items to enumerate.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when <paramref name="count" /> is less than zero.</exception>
    static member truncate: count: int -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, yields elements of the underlying sequence while the
    /// given function <paramref name="predicate" /> returns <see cref="true" />, and then returns no further elements.
    /// Stops consuming the source and yielding items as soon as the predicate returns <c>false</c>.
    /// (see also <see cref="TaskSeq.takeWhileInclusive" />).
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.takeWhileAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function that evaluates to false when no more items should be returned.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member takeWhile: predicate: ('T -> bool) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, yields elements of the underlying sequence while the
    /// given asynchronous function <paramref name="predicate" /> returns <see cref="true" />, and then returns no further elements.
    /// Stops consuming the source and yielding items as soon as the predicate returns <c>false</c>.
    /// (see also <see cref="TaskSeq.takeWhileInclusiveAsync" />).
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.takeWhile" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to false when no more items should be returned.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member takeWhileAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, yields elements of the underlying sequence until the given
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
    static member takeWhileInclusive: predicate: ('T -> bool) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, yields elements of the underlying sequence until the given
    /// asynchronous function <paramref name="predicate" /> returns <see cref="false" />, returns that element
    /// and then returns no further elements (see also <see cref="TaskSeq.takeWhileAsync" />). This function returns
    /// at least one element of a non-empty sequence, or the empty task sequence if the input is empty.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.takeWhileInclusive" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to false when no more items should be returned.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member takeWhileInclusiveAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, skips elements of the underlying sequence while the
    /// given function <paramref name="predicate" /> returns <see cref="true" />, and then yields the remaining
    /// elements. Elements where the predicate returns <see cref="false" /> are propagated, which means that this
    /// function may not skip any elements (see also <see cref="TaskSeq.skipWhileInclusive" />).
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.skipWhileAsync" />.
    /// </summary>
    ///
    /// <param name="predicate">A function that evaluates to false when no more items should be skipped.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member skipWhile: predicate: ('T -> bool) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, skips elements of the underlying sequence while the
    /// given asynchronous function <paramref name="predicate" /> returns <see cref="true" />, and then yields the
    /// remaining elements. Elements where the predicate returns <see cref="false" /> are propagated, which means that this
    /// function may not skip any elements (see also <see cref="TaskSeq.skipWhileInclusiveAsync" />).
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.skipWhile" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to false when no more items should be skipped.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member skipWhileAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, skips elements of the underlying sequence until the given
    /// function <paramref name="predicate" /> returns <see cref="false" />, <i>also skips that element</i>
    /// and then yields the remaining elements (see also <see cref="TaskSeq.skipWhile" />). It will thus always skip
    /// at least one element of a non-empty sequence, or returns the empty task sequence if the input is empty.
    /// If <paramref name="predicate" /> is asynchronous, consider using <see cref="TaskSeq.skipWhileInclusiveAsync" />.
    /// </summary>`
    ///
    /// <param name="predicate">A function that evaluates to false for the final item to be skipped.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member skipWhileInclusive: predicate: ('T -> bool) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Returns a task sequence that, when iterated, skips elements of the underlying sequence until the given
    /// function <paramref name="predicate" /> returns <see cref="false" />, <i>also skips that element</i>
    /// and then yields the remaining elements (see also <see cref="TaskSeq.skipWhileAsync" />). It will thus always skip
    /// at least one element of a non-empty sequence, or returns the empty task sequence if the input is empty.
    /// If <paramref name="predicate" /> is synchronous, consider using <see cref="TaskSeq.skipWhileInclusive" />.
    /// </summary>
    ///
    /// <param name="predicate">An asynchronous function that evaluates to false for the final item to be skipped.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member skipWhileInclusiveAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to successive elements, returning the first result where
    /// the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is asynchronous, consider using <see cref="TaskSeq.tryPickAsync" />.
    /// </summary>
    /// <param name="chooser">A function to transform items of type <paramref name="'T" /> into options of type <paramref name="'U" />.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The chosen element or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryPick: chooser: ('T -> 'U option) -> source: TaskSeq<'T> -> Task<'U option>

    /// <summary>
    /// Applies the given asynchronous function <paramref name="chooser" /> to successive elements, returning the first result where
    /// the function returns <see cref="Some(x)" />.
    /// If <paramref name="chooser" /> is synchronous, consider using <see cref="TaskSeq.tryPick" />.
    /// </summary>
    /// <param name="chooser">An asynchronous function to transform items of type <paramref name="'T" /> into options of type <paramref name="'U" />.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The chosen element or <see cref="None" />.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member tryPickAsync: chooser: ('T -> #Task<'U option>) -> source: TaskSeq<'T> -> Task<'U option>

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
    static member tryFind: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<'T option>

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
    static member tryFindAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<'T option>

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
    static member tryFindIndex: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<int option>

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
    static member tryFindIndexAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<int option>

    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to successive elements, returning the first result where
    /// the function returns <see cref="Some(x)" />. Throws an exception if none is found.
    /// If <paramref name="chooser" /> is asynchronous, consider using <see cref="TaskSeq.pickAsync" />.
    /// </summary>
    ///
    /// <param name="chooser">A function to transform items of type <paramref name="'T" /> into options of type <paramref name="'U" />.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The selected element.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:KeyNotFoundException">Thrown when every item of the sequence evaluates to <see cref="None" /> when the given function is applied.</exception>
    static member pick: chooser: ('T -> 'U option) -> source: TaskSeq<'T> -> Task<'U>

    /// <summary>
    /// Applies the given asynchronous function <paramref name="chooser" /> to successive elements, returning the first result where
    /// the function returns <see cref="Some(x)" />. Throws an exception if none is found.
    /// If <paramref name="chooser" /> is synchronous, consider using <see cref="TaskSeq.pick" />.
    /// </summary>
    ///
    /// <param name="chooser">An asynchronous function to transform items of type <paramref name="'T" /> into options of type <paramref name="'U" />.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The selected element.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:KeyNotFoundException">Thrown when every item of the sequence evaluates to <see cref="None" /> when the given function is applied.</exception>
    static member pickAsync: chooser: ('T -> #Task<'U option>) -> source: TaskSeq<'T> -> Task<'U>

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
    static member find: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<'T>

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
    static member findAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<'T>

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
    static member findIndex: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<int>

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
    static member findIndexAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<int>

    /// <summary>
    /// Tests if the sequence contains the specified element. Returns <see cref="true" />
    /// if <paramref name="source" /> contains the specified element; <see cref="false" />
    /// otherwise. The input task sequence is only evaluated until the first element that matches the value.
    /// </summary>
    ///
    /// <param name="value">The value to locate in the input sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns><see cref="true" /> if the input sequence contains the specified element; <see cref="false" /> otherwise.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member contains<'T when 'T: equality> : value: 'T -> source: TaskSeq<'T> -> Task<bool>

    /// <summary>
    /// Tests if any element of the task sequence in <paramref name="source" /> satisfies the given <paramref name="predicate" />. The function
    /// is applied to the elements of the input task sequence. If any application returns <see cref="true" /> then the overall result
    /// is <see cref="true" /> and no further elements are evaluated and tested.
    /// Otherwise, <see cref="false" /> is returned.
    /// </summary>
    ///
    /// <param name="predicate">A function to test each item of the input sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns><see cref="true" /> if any result from the predicate is true; <see cref="false" /> otherwise.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member exists: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<bool>

    /// <summary>
    /// Tests if any element of the task sequence in <paramref name="source" /> satisfies the given asynchronous <paramref name="predicate" />.
    /// The function is applied to the elements of the input task sequence. If any application returns <see cref="true" /> then the overall result
    /// is <see cref="true" /> and no further elements are evaluated and tested.
    /// Otherwise, <see cref="false" /> is returned.
    /// </summary>
    ///
    /// <param name="predicate">A function to test each item of the input sequence.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns><see cref="true" /> if any result from the predicate is true; <see cref="false" /> otherwise.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member existsAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<bool>

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
    static member except<'T when 'T: equality> : itemsToExclude: TaskSeq<'T> -> source: TaskSeq<'T> -> TaskSeq<'T>

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
    static member exceptOfSeq<'T when 'T: equality> : itemsToExclude: seq<'T> -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Combines the two task sequences into a new task sequence of pairs. The two sequences need not have equal lengths:
    /// when one sequence is exhausted any remaining elements in the other sequence are ignored.
    /// </summary>
    ///
    /// <param name="source1">The first input task sequence.</param>
    /// <param name="source2">The second input task sequence.</param>
    /// <returns>The result task sequence of tuples.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the two input task sequences is null.</exception>
    static member zip: source1: TaskSeq<'T> -> source2: TaskSeq<'U> -> TaskSeq<'T * 'U>

    /// <summary>
    /// Applies the function <paramref name="folder" /> to each element in the task sequence, threading an accumulator
    /// argument of type <typeref name="'State" /> through the computation.  If the input function is <paramref name="f" /> and the elements are <paramref name="i0...iN" />
    /// then computes<paramref name="f (... (f s i0)...) iN" />.
    /// If the accumulator function <paramref name="folder" /> is asynchronous, consider using <see cref="TaskSeq.foldAsync" />.
    /// argument of type <paramref name="'State" /> through the computation.  If the input function is <paramref name="f" /> and the elements are <paramref name="i0...iN" />
    /// then computes <paramref name="f (... (f s i0)...) iN" />.
    /// If the accumulator function <paramref name="folder" /> is asynchronous, consider using <see cref="TaskSeq.foldAsync" />.
    /// </summary>
    ///
    /// <param name="folder">A function that updates the state with each element from the sequence.</param>
    /// <param name="state">The initial state.</param>
    /// <param name="source">The input sequence.</param>
    /// <returns>The state object after the folding function is applied to each element of the sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    static member fold: folder: ('State -> 'T -> 'State) -> state: 'State -> source: TaskSeq<'T> -> Task<'State>

    /// <summary>
    /// Applies the asynchronous function <paramref name="folder" /> to each element in the task sequence, threading an accumulator
    /// argument of type <paramref name="'State" /> through the computation.  If the input function is <paramref name="f" /> and the elements are <paramref name="i0...iN" />
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
        folder: ('State -> 'T -> #Task<'State>) -> state: 'State -> source: TaskSeq<'T> -> Task<'State>

    /// <summary>
    /// Return a new task sequence with a new item inserted before the given index.
    /// </summary>
    ///
    /// <param name="index">The index where the item should be inserted.</param>
    /// <param name="value">The value to insert.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The result task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when index is below 0 or greater than source length.</exception>
    static member insertAt: index: int -> value: 'T -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Return a new task sequence with the new items inserted before the given index.
    /// </summary>
    ///
    /// <param name="index">The index where the items should be inserted.</param>
    /// <param name="value">The values to insert.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The result task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when index is below 0 or greater than source length.</exception>
    static member insertManyAt: index: int -> values: TaskSeq<'T> -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Return a new task sequence with the item at the given index removed.
    /// </summary>
    ///
    /// <param name="index">The index where the item should be removed.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The result task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when index is below 0 or greater than source length.</exception>
    static member removeAt: index: int -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Return a new task sequence with the number of items starting at a given index removed.
    /// If <paramref name="count" /> is negative or zero, no items are removed. If <paramref name="index" />
    /// + <paramref name="count" /> is greater than source length, but <paramref name="index" /> is not, then
    /// all items until end of sequence are removed.
    /// </summary>
    ///
    /// <param name="index">The index where the items should be removed.</param>
    /// <param name="count">The number of items to remove.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The result task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when index is below 0 or greater than source length.</exception>
    static member removeManyAt: index: int -> count: int -> source: TaskSeq<'T> -> TaskSeq<'T>

    /// <summary>
    /// Return a new task sequence with the item at a given index set to the new value.
    /// </summary>
    ///
    /// <param name="index">The index of the item to be replaced.</param>
    /// <param name="value">The new value.</param>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The result task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input task sequence is null.</exception>
    /// <exception cref="T:ArgumentException">Thrown when index is below 0 or greater than source length.</exception>
    static member updateAt: index: int -> value: 'T -> source: TaskSeq<'T> -> TaskSeq<'T>
