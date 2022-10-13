namespace FSharpy
    
    module TaskSeq =
        
        /// Initialize an empty taskSeq.
        val empty<'T> : System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
        val toList: t: TaskSeqBuilders.taskSeq<'T> -> 'T list
        
        /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
        val toArray: taskSeq: TaskSeqBuilders.taskSeq<'T> -> 'T[]
        
        /// Returns taskSeq as a seq, similar to Seq.cached. This function is blocking until the sequence is exhausted and will properly dispose of the resources.
        val toSeqCached: taskSeq: TaskSeqBuilders.taskSeq<'T> -> seq<'T>
        
        /// Unwraps the taskSeq as a Task<array<_>>. This function is non-blocking.
        val toArrayAsync:
          taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Threading.Tasks.Task<'a[]>
        
        /// Unwraps the taskSeq as a Task<list<_>>. This function is non-blocking.
        val toListAsync:
          taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Threading.Tasks.Task<'a list>
        
        /// Unwraps the taskSeq as a Task<ResizeArray<_>>. This function is non-blocking.
        val toResizeArrayAsync:
          taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Threading.Tasks.Task<ResizeArray<'a>>
        
        /// Unwraps the taskSeq as a Task<IList<_>>. This function is non-blocking.
        val toIListAsync:
          taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Threading.Tasks.Task<System.Collections.Generic.IList<'a>>
        
        /// Unwraps the taskSeq as a Task<seq<_>>. This function is non-blocking,
        /// exhausts the sequence and caches the results of the tasks in the sequence.
        val toSeqCachedAsync:
          taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Threading.Tasks.Task<seq<'a>>
        
        /// Create a taskSeq of an array.
        val ofArray:
          array: 'T[] -> System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Create a taskSeq of a list.
        val ofList:
          list: 'T list -> System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Create a taskSeq of a seq.
        val ofSeq:
          sequence: seq<'T> -> System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Create a taskSeq of a ResizeArray, aka List.
        val ofResizeArray:
          data: ResizeArray<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Create a taskSeq of a sequence of tasks, that may already have hot-started.
        val ofTaskSeq:
          sequence: seq<#System.Threading.Tasks.Task<'T>> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Create a taskSeq of a list of tasks, that may already have hot-started.
        val ofTaskList:
          list: #System.Threading.Tasks.Task<'T> list ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Create a taskSeq of an array of tasks, that may already have hot-started.
        val ofTaskArray:
          array: #System.Threading.Tasks.Task<'T> array ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Create a taskSeq of a seq of async.
        val ofAsyncSeq:
          sequence: seq<Async<'T>> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Create a taskSeq of a list of async.
        val ofAsyncList:
          list: Async<'T> list ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Create a taskSeq of an array of async.
        val ofAsyncArray:
          array: Async<'T> array ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        /// Iterates over the taskSeq applying the action function to each item. This function is non-blocking
        /// exhausts the sequence as soon as the task is evaluated.
        val iter:
          action: ('a -> unit) ->
            taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Threading.Tasks.Task<unit>
        
        /// Iterates over the taskSeq applying the action function to each item. This function is non-blocking,
        /// exhausts the sequence as soon as the task is evaluated.
        val iteri:
          action: (int -> 'a -> unit) ->
            taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Threading.Tasks.Task<unit>
        
        /// Iterates over the taskSeq applying the async action to each item. This function is non-blocking
        /// exhausts the sequence as soon as the task is evaluated.
        val iterAsync:
          action: ('a -> #System.Threading.Tasks.Task<unit>) ->
            taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Threading.Tasks.Task<unit>
        
        /// Iterates over the taskSeq, applying the async action to each item. This function is non-blocking,
        /// exhausts the sequence as soon as the task is evaluated.
        val iteriAsync:
          action: (int -> 'a -> #System.Threading.Tasks.Task<unit>) ->
            taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Threading.Tasks.Task<unit>
        
        /// Maps over the taskSeq, applying the mapper function to each item. This function is non-blocking.
        val map:
          mapper: ('T -> 'U) ->
            taskSeq: TaskSeqBuilders.taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        /// Maps over the taskSeq with an index, applying the mapper function to each item. This function is non-blocking.
        val mapi:
          mapper: (int -> 'T -> 'U) ->
            taskSeq: TaskSeqBuilders.taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        /// Maps over the taskSeq, applying the async mapper function to each item. This function is non-blocking.
        val mapAsync:
          mapper: ('a -> #System.Threading.Tasks.Task<'c>) ->
            taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Collections.Generic.IAsyncEnumerable<'c>
        
        /// Maps over the taskSeq with an index, applying the async mapper function to each item. This function is non-blocking.
        val mapiAsync:
          mapper: (int -> 'a -> #System.Threading.Tasks.Task<'c>) ->
            taskSeq: TaskSeqBuilders.taskSeq<'a> ->
            System.Collections.Generic.IAsyncEnumerable<'c>
        
        /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
        val collect:
          binder: ('T -> #System.Collections.Generic.IAsyncEnumerable<'U>) ->
            taskSeq: TaskSeqBuilders.taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        /// Applies the given function to the items in the taskSeq and concatenates all the results in order.
        val collectSeq:
          binder: ('T -> #seq<'U>) ->
            taskSeq: TaskSeqBuilders.taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        /// Applies the given async function to the items in the taskSeq and concatenates all the results in order.
        val collectAsync:
          binder: ('T -> #System.Threading.Tasks.Task<'b>) ->
            taskSeq: TaskSeqBuilders.taskSeq<'T> -> TaskSeqBuilders.taskSeq<'U>
            when 'b :> System.Collections.Generic.IAsyncEnumerable<'U>
        
        /// Applies the given async function to the items in the taskSeq and concatenates all the results in order.
        val collectSeqAsync:
          binder: ('T -> #System.Threading.Tasks.Task<'b>) ->
            taskSeq: TaskSeqBuilders.taskSeq<'T> -> TaskSeqBuilders.taskSeq<'U>
            when 'b :> seq<'U>
        
        /// Zips two task sequences, returning a taskSeq of the tuples of each sequence, in order. May raise ArgumentException
        /// if the sequences are or unequal length.
        val zip:
          taskSeq1: TaskSeqBuilders.taskSeq<'a> ->
            taskSeq2: TaskSeqBuilders.taskSeq<'b> ->
            System.Collections.Generic.IAsyncEnumerable<'a * 'b>
        
        /// Applies a function to each element of the task sequence, threading an accumulator argument through the computation.
        val fold:
          folder: ('a -> 'b -> 'a) ->
            state: 'a ->
            taskSeq: TaskSeqBuilders.taskSeq<'b> ->
            System.Threading.Tasks.Task<'a>
        
        /// Applies an async function to each element of the task sequence, threading an accumulator argument through the computation.
        val foldAsync:
          folder: ('a -> 'b -> #System.Threading.Tasks.Task<'a>) ->
            state: 'a ->
            taskSeq: TaskSeqBuilders.taskSeq<'b> ->
            System.Threading.Tasks.Task<'a>

