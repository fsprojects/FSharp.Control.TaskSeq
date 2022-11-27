namespace FSharp.Control
    
    module TaskSeq =
        
        val empty<'T> : System.Collections.Generic.IAsyncEnumerable<'T>
        
        val singleton:
          source: 'T -> System.Collections.Generic.IAsyncEnumerable<'T>
        
        val isEmpty: source: taskSeq<'T> -> System.Threading.Tasks.Task<bool>
        
        val toList: source: taskSeq<'T> -> 'T list
        
        val toArray: source: taskSeq<'T> -> 'T[]
        
        val toSeq: source: taskSeq<'T> -> seq<'T>
        
        val toArrayAsync:
          source: taskSeq<'T> -> System.Threading.Tasks.Task<'T[]>
        
        val toListAsync:
          source: taskSeq<'T> -> System.Threading.Tasks.Task<'T list>
        
        val toResizeArrayAsync:
          source: taskSeq<'T> -> System.Threading.Tasks.Task<ResizeArray<'T>>
        
        val toIListAsync:
          source: taskSeq<'T> ->
            System.Threading.Tasks.Task<System.Collections.Generic.IList<'T>>
        
        val ofArray:
          source: 'T[] -> System.Collections.Generic.IAsyncEnumerable<'T>
        
        val ofList:
          source: 'T list -> System.Collections.Generic.IAsyncEnumerable<'T>
        
        val ofSeq:
          source: seq<'T> -> System.Collections.Generic.IAsyncEnumerable<'T>
        
        val ofResizeArray:
          source: ResizeArray<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val ofTaskSeq:
          source: seq<#System.Threading.Tasks.Task<'T>> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val ofTaskList:
          source: #System.Threading.Tasks.Task<'T> list ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val ofTaskArray:
          source: #System.Threading.Tasks.Task<'T> array ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val ofAsyncSeq:
          source: seq<Async<'T>> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val ofAsyncList:
          source: Async<'T> list ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val ofAsyncArray:
          source: Async<'T> array ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val length: source: taskSeq<'T> -> System.Threading.Tasks.Task<int>
        
        val lengthOrMax:
          max: int -> source: taskSeq<'T> -> System.Threading.Tasks.Task<int>
        
        val lengthBy:
          predicate: ('T -> bool) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<int>
        
        val lengthByAsync:
          predicate: ('T -> #System.Threading.Tasks.Task<bool>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<int>
        
        val init:
          count: int ->
            initializer: (int -> 'T) ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val initInfinite:
          initializer: (int -> 'T) ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val initAsync:
          count: int ->
            initializer: (int -> #System.Threading.Tasks.Task<'T>) ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val initInfiniteAsync:
          initializer: (int -> #System.Threading.Tasks.Task<'T>) ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val delay:
          generator: (unit -> taskSeq<'T>) ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val concat:
          sources: taskSeq<#taskSeq<'T>> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val append:
          source1: #taskSeq<'T> ->
            source2: #taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val appendSeq:
          source1: #taskSeq<'T> ->
            source2: #seq<'T> -> System.Collections.Generic.IAsyncEnumerable<'T>
        
        val prependSeq:
          source1: #seq<'T> ->
            source2: #taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val cast: source: taskSeq<obj> -> taskSeq<'T>
        
        val box:
          source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<obj>
        
        val unbox: source: taskSeq<obj> -> taskSeq<'U> when 'U: struct
        
        val iter:
          action: ('T -> unit) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<unit>
        
        val iteri:
          action: (int -> 'T -> unit) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<unit>
        
        val iterAsync:
          action: ('T -> #System.Threading.Tasks.Task<unit>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<unit>
        
        val iteriAsync:
          action: (int -> 'T -> #System.Threading.Tasks.Task<unit>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<unit>
        
        val map:
          mapper: ('T -> 'U) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        val mapi:
          mapper: (int -> 'T -> 'U) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        val mapAsync:
          mapper: ('T -> #System.Threading.Tasks.Task<'U>) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        val mapiAsync:
          mapper: (int -> 'T -> #System.Threading.Tasks.Task<'U>) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        val collect:
          binder: ('T -> #System.Collections.Generic.IAsyncEnumerable<'U>) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        val collectSeq:
          binder: ('T -> #seq<'U>) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        val collectAsync:
          binder: ('T -> #System.Threading.Tasks.Task<'TSeqU>) ->
            source: taskSeq<'T> -> taskSeq<'U>
            when 'TSeqU :> System.Collections.Generic.IAsyncEnumerable<'U>
        
        val collectSeqAsync:
          binder: ('T -> #System.Threading.Tasks.Task<'SeqU>) ->
            source: taskSeq<'T> -> taskSeq<'U> when 'SeqU :> seq<'U>
        
        val tryHead:
          source: taskSeq<'T> -> System.Threading.Tasks.Task<'T option>
        
        val head: source: taskSeq<'T> -> System.Threading.Tasks.Task<'T>
        
        val tryLast:
          source: taskSeq<'T> -> System.Threading.Tasks.Task<'T option>
        
        val last: source: taskSeq<'T> -> System.Threading.Tasks.Task<'T>
        
        val tryTail:
          source: taskSeq<'T> ->
            System.Threading.Tasks.Task<System.Collections.Generic.IAsyncEnumerable<'T> option>
        
        val tail:
          source: taskSeq<'T> ->
            System.Threading.Tasks.Task<System.Collections.Generic.IAsyncEnumerable<'T>>
        
        val tryItem:
          index: int ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'T option>
        
        val item:
          index: int -> source: taskSeq<'T> -> System.Threading.Tasks.Task<'T>
        
        val tryExactlyOne:
          source: taskSeq<'T> -> System.Threading.Tasks.Task<'T option>
        
        val exactlyOne: source: taskSeq<'T> -> System.Threading.Tasks.Task<'T>
        
        val indexed:
          source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<int * 'T>
        
        val choose:
          chooser: ('T -> 'U option) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        val chooseAsync:
          chooser: ('T -> #System.Threading.Tasks.Task<'U option>) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'U>
        
        val filter:
          predicate: ('T -> bool) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val filterAsync:
          predicate: ('T -> #System.Threading.Tasks.Task<bool>) ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'T>
        
        val tryPick:
          chooser: ('T -> 'U option) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'U option>
        
        val tryPickAsync:
          chooser: ('T -> #System.Threading.Tasks.Task<'U option>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'U option>
        
        val tryFind:
          predicate: ('T -> bool) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'T option>
        
        val tryFindAsync:
          predicate: ('T -> #System.Threading.Tasks.Task<bool>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'T option>
        
        val tryFindIndex:
          predicate: ('T -> bool) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<int option>
        
        val tryFindIndexAsync:
          predicate: ('T -> #System.Threading.Tasks.Task<bool>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<int option>
        
        val except:
          itemsToExclude: taskSeq<'T> ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'T> when 'T: equality
        
        val exceptOfSeq:
          itemsToExclude: seq<'T> ->
            source: taskSeq<'T> ->
            System.Collections.Generic.IAsyncEnumerable<'T> when 'T: equality
        
        val exists:
          predicate: ('T -> bool) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<bool>
        
        val existsAsync:
          predicate: ('T -> #System.Threading.Tasks.Task<bool>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<bool>
        
        val contains:
          value: 'T -> source: taskSeq<'T> -> System.Threading.Tasks.Task<bool>
            when 'T: equality
        
        val pick:
          chooser: ('T -> 'U option) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'U>
        
        val pickAsync:
          chooser: ('T -> #System.Threading.Tasks.Task<'U option>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'U>
        
        val find:
          predicate: ('T -> bool) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'T>
        
        val findAsync:
          predicate: ('T -> #System.Threading.Tasks.Task<bool>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'T>
        
        val findIndex:
          predicate: ('T -> bool) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<int>
        
        val findIndexAsync:
          predicate: ('T -> #System.Threading.Tasks.Task<bool>) ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<int>
        
        val zip:
          source1: taskSeq<'T> ->
            source2: taskSeq<'U> ->
            System.Collections.Generic.IAsyncEnumerable<'T * 'U>
        
        val fold:
          folder: ('State -> 'T -> 'State) ->
            state: 'State ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'State>
        
        val foldAsync:
          folder: ('State -> 'T -> #System.Threading.Tasks.Task<'State>) ->
            state: 'State ->
            source: taskSeq<'T> -> System.Threading.Tasks.Task<'State>

