namespace FSharp.Control

#nowarn "1204"

module TaskSeq =
    open System.Collections.Generic
    open System.Threading.Tasks
    open FSharp.Control.TaskSeqBuilders

    /// Initialize an empty taskSeq.
    val empty<'T> : taskSeq<'T>

    /// <summary>
    /// Returns <see cref="true" /> if the task sequence contains no elements, <see cref="false" /> otherwise.
    /// </summary>
    val isEmpty: source: taskSeq<'T> -> Task<bool>

    /// <summary>
    /// Returns the length of the sequence. This operation requires the whole sequence to be evaluated and
    /// should not be used on potentially infinite sequences, see <see cref="lengthOrMax" /> for an alternative.
    /// </summary>
    val length: source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence, or <paramref name="max" />, whichever comes first. This operation requires the task sequence
    /// to be evaluated in full, or until <paramref name="max" /> items have been processed. Use this method instead of
    /// <see cref="TaskSeq.length" /> if you want to prevent too many items to be evaluated, or if the sequence is potentially infinite.
    /// </summary>
    val lengthOrMax: max: int -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence of all items for which the <paramref name="predicate" /> returns true.
    /// This operation requires the whole sequence to be evaluated and should not be used on potentially infinite sequences.
    /// </summary>
    val lengthBy: predicate: ('T -> bool) -> source: taskSeq<'T> -> Task<int>

    /// <summary>
    /// Returns the length of the sequence of all items for which the <paramref name="predicate" /> returns true.
    /// This operation requires the whole sequence to be evaluated and should not be used on potentially infinite sequences.
    /// If <paramref name="predicate" /> does not need to be asynchronous, consider using <see cref="TaskSeq.lengthBy" />.
    /// </summary>
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
    /// <param name="sources">The input enumeration-of-enumerations.</param>
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
    val append: source1: #taskSeq<'T> -> source2: #taskSeq<'T> -> taskSeq<'T>

    /// <summary>
    /// Concatenates a task sequence <paramref name="source1" /> with a non-async F# <see cref="seq" /> in <paramref name="source2" />
    /// and returns a single task sequence.
    /// </summary>
    ///
    /// <param name="source1">The input task sequence.</param>
    /// <param name="source2">The input F# <see cref="seq" /> sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input sequences is null.</exception>
    val appendSeq: source1: #taskSeq<'T> -> source2: #seq<'T> -> taskSeq<'T>

    /// <summary>
    /// Concatenates a non-async F# <see cref="seq" /> in <paramref name="source1" /> with a task sequence in <paramref name="source2" />
    /// and returns a single task sequence.
    /// </summary>
    ///
    /// <param name="source1">The input F# <see cref="seq" /> sequence.</param>
    /// <param name="source2">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when either of the input sequences is null.</exception>
    val prependSeq: source1: #seq<'T> -> source2: #taskSeq<'T> -> taskSeq<'T>

    /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
    val toList: source: taskSeq<'T> -> 'T list

    /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
    val toArray: source: taskSeq<'T> -> 'T[]

    /// <summary>
    /// Returns the task sequence <paramref name="source" /> as an F# <see cref="seq" />, that is, an
    /// <see cref="IEnumerable&lt;'T>" />. This function is blocking at each <see cref="yield" />, but otherwise
    /// acts as a normal delay-executed sequence.
    /// It will then dispose of the resources.
    /// </summary>
    ///
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence.</returns>
    val toSeq: source: taskSeq<'T> -> seq<'T>

    /// Unwraps the taskSeq as a Task<array<_>>. This function is non-blocking.
    val toArrayAsync: source: taskSeq<'T> -> Task<'T[]>

    /// Unwraps the taskSeq as a Task<list<_>>. This function is non-blocking.
    val toListAsync: source: taskSeq<'T> -> Task<'T list>

    /// Unwraps the taskSeq as a Task<ResizeArray<_>>. This function is non-blocking.
    val toResizeArrayAsync: source: taskSeq<'T> -> Task<ResizeArray<'T>>

    /// Unwraps the taskSeq as a Task<IList<_>>. This function is non-blocking.
    val toIListAsync: source: taskSeq<'T> -> Task<IList<'T>>

    /// Create a taskSeq of an array.
    val ofArray: source: 'T[] -> taskSeq<'T>

    /// Create a taskSeq of a list.
    val ofList: source: 'T list -> taskSeq<'T>

    /// Create a taskSeq of a seq.
    val ofSeq: source: seq<'T> -> taskSeq<'T>

    /// Create a taskSeq of a ResizeArray, aka List.
    val ofResizeArray: source: ResizeArray<'T> -> taskSeq<'T>

    /// Create a taskSeq of a sequence of tasks, that may already have hot-started.
    val ofTaskSeq: source: seq<#Task<'T>> -> taskSeq<'T>

    /// Create a taskSeq of a list of tasks, that may already have hot-started.
    val ofTaskList: source: #Task<'T> list -> taskSeq<'T>

    /// Create a taskSeq of an array of tasks, that may already have hot-started.
    val ofTaskArray: source: #Task<'T> array -> taskSeq<'T>

    /// Create a taskSeq of a seq of async.
    val ofAsyncSeq: source: seq<Async<'T>> -> taskSeq<'T>

    /// Create a taskSeq of a list of async.
    val ofAsyncList: source: Async<'T> list -> taskSeq<'T>

    /// Create a taskSeq of an array of async.
    val ofAsyncArray: source: Async<'T> array -> taskSeq<'T>

    /// <summary>
    /// Boxes as type <see cref="obj" /> each item in the <paramref name="source" /> sequence asynchyronously.
    /// </summary>
    val box: source: taskSeq<'T> -> taskSeq<obj>

    /// <summary>
    /// Unboxes to the target type <see cref="'U" /> each item in the <paramref name="source" /> sequence asynchyronously.
    /// The target type must be a <see cref="struct" /> or a built-in value type.
    /// </summary>
    /// <exception cref="InvalidCastException">Thrown when the function is unable to cast an item to the target type.</exception>
    val unbox<'U when 'U: struct> : source: taskSeq<obj> -> taskSeq<'U>

    /// <summary>
    /// Casts each item in the untyped <paramref name="source" /> sequence asynchyronously. If your types are boxed struct types
    /// it is recommended to use <see cref="TaskSeq.unbox" /> instead.
    /// </summary>
    /// <exception cref="InvalidCastException">Thrown when the function is unable to cast an item to the target type.</exception>
    val cast: source: taskSeq<obj> -> taskSeq<'T>

    /// Iterates over the taskSeq applying the action function to each item. This function is non-blocking
    /// exhausts the sequence as soon as the task is evaluated.
    val iter: action: ('T -> unit) -> source: taskSeq<'T> -> Task<unit>

    /// Iterates over the taskSeq applying the action function to each item. This function is non-blocking,
    /// exhausts the sequence as soon as the task is evaluated.
    val iteri: action: (int -> 'T -> unit) -> source: taskSeq<'T> -> Task<unit>

    /// Iterates over the taskSeq applying the async action to each item. This function is non-blocking
    /// exhausts the sequence as soon as the task is evaluated.
    val iterAsync: action: ('T -> #Task<unit>) -> source: taskSeq<'T> -> Task<unit>

    /// Iterates over the taskSeq, applying the async action to each item. This function is non-blocking,
    /// exhausts the sequence as soon as the task is evaluated.
    val iteriAsync: action: (int -> 'T -> #Task<unit>) -> source: taskSeq<'T> -> Task<unit>

    /// Maps over the taskSeq, applying the mapper function to each item. This function is non-blocking.
    val map: mapper: ('T -> 'U) -> source: taskSeq<'T> -> taskSeq<'U>

    /// <summary>
    /// Builds a new task sequence whose elements are the corresponding elements of the input task
    /// sequence <paramref name="source" /> paired with the integer index (from 0) of each element.
    /// Does not evaluate the input sequence until requested.
    /// </summary>
    /// <param name="source">The input task sequence.</param>
    /// <returns>The resulting task sequence of tuples.</returns>
    /// <exception cref="T:ArgumentNullException">Thrown when the input sequence is null.</exception>
    val indexed: source: taskSeq<'T> -> taskSeq<int * 'T>

    /// Maps over the taskSeq with an index, applying the mapper function to each item. This function is non-blocking.
    val mapi: mapper: (int -> 'T -> 'U) -> source: taskSeq<'T> -> taskSeq<'U>

    /// Maps over the taskSeq, applying the async mapper function to each item. This function is non-blocking.
    val mapAsync: mapper: ('T -> #Task<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// Maps over the taskSeq with an index, applying the async mapper function to each item. This function is non-blocking.
    val mapiAsync: mapper: (int -> 'T -> #Task<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
    val collect: binder: ('T -> #taskSeq<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
    val collectSeq: binder: ('T -> #seq<'U>) -> source: taskSeq<'T> -> taskSeq<'U>

    /// Applies the given async function to the items in the taskSeq and concatenates all the results in order.
    val collectAsync: binder: ('T -> #Task<'TSeqU>) -> source: taskSeq<'T> -> taskSeq<'U> when 'TSeqU :> taskSeq<'U>

    /// Applies the given async function to the items in the taskSeq and concatenates all the results in order.
    val collectSeqAsync: binder: ('T -> #Task<'SeqU>) -> source: taskSeq<'T> -> taskSeq<'U> when 'SeqU :> seq<'U>

    /// <summary>
    /// Returns the first element of the task sequence from <paramref name="source" />, or <see cref="None" /> if the sequence is empty.
    /// </summary>
    val tryHead: source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the first elementof the task sequence from <paramref name="source" />
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    val head: source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the whole task sequence from <paramref name="source" />, minus its first element, or <see cref="None" /> if the sequence is empty.
    /// </summary>
    val tryTail: source: taskSeq<'T> -> Task<taskSeq<'T> option>

    /// <summary>
    /// Returns the whole task sequence from <paramref name="source" />, minus its first element.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    val tail: source: taskSeq<'T> -> Task<taskSeq<'T>>

    /// <summary>
    /// Returns the last element of the task sequence from <paramref name="source" />, or <see cref="None" /> if the sequence is empty.
    /// </summary>
    val tryLast: source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the last element of the <see cref="taskSeq" />.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence is empty.</exception>
    val last: source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the nth element of the <see cref="taskSeq" />, or <see cref="None" /> if the sequence
    /// does not contain enough elements, or if <paramref name="index" /> is negative.
    /// Parameter <paramref name="index" /> is zero-based, that is, the value 0 returns the first element.
    /// </summary>
    val tryItem: index: int -> source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the nth element of the <see cref="taskSeq" />, or <see cref="None" /> if the sequence
    /// does not contain enough elements, or if <paramref name="index" /> is negative.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the sequence has insufficient length or
    /// <paramref name="index" /> is negative.</exception>
    val item: index: int -> source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Returns the only element of the task sequence, or <see cref="None" /> if the sequence is empty of
    /// contains more than one element.
    /// </summary>
    val tryExactlyOne: source: taskSeq<'T> -> Task<'T option>

    /// <summary>
    /// Returns the only element of the task sequence.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the input sequence does not contain precisely one element.</exception>
    val exactlyOne: source: taskSeq<'T> -> Task<'T>

    /// <summary>
    /// Applies the given function <paramref name="chooser" /> to each element of the task sequence. Returns
    /// a sequence comprised of the results "x" for each element where
    /// the function returns <c>Some(x)</c>.
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



[<AutoOpen>]
module AsyncSeqExtensions =

    val WhileDynamic:
        sm: byref<TaskStateMachine<'Data>> *
        condition: (unit -> System.Threading.Tasks.ValueTask<bool>) *
        body: TaskCode<'Data, unit> ->
            bool

    val WhileBodyDynamicAux:
        sm: byref<TaskStateMachine<'Data>> *
        condition: (unit -> System.Threading.Tasks.ValueTask<bool>) *
        body: TaskCode<'Data, unit> *
        rf: TaskResumptionFunc<'Data> ->
            bool

    type AsyncBuilder with

        member For: tasksq: System.Collections.Generic.IAsyncEnumerable<'T> * action: ('T -> Async<unit>) -> Async<unit>

    type TaskBuilder with

        member inline WhileAsync:
            condition: (unit -> System.Threading.Tasks.ValueTask<bool>) * body: TaskCode<'TOverall, unit> ->
                TaskCode<'TOverall, unit>

        member inline For:
            tasksq: System.Collections.Generic.IAsyncEnumerable<'T> * body: ('T -> TaskCode<'TOverall, unit>) ->
                TaskCode<'TOverall, unit>
