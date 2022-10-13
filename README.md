# TaskSeq
An implementation `IAsyncEnumerableM<'T>` as a `taskSeq` CE for F# with accompanying `TaskSeq` module.

## In progress!!!

It's based on [Don Symes `taskSeq.fs`](https://github.com/dotnet/fsharp/blob/d5312aae8aad650f0043f055bb14c3aa8117e12e/tests/benchmarks/CompiledCodeBenchmarks/TaskPerf/TaskPerf/taskSeq.fs)
but expanded with useful utility functions and a few extra binding overloads.

## Current set of `TaskSeq` utility functions

The following is the current surface area of the `TaskSeq` utility functions. This is just a dump of the signatures, proper 
documentation will be added soon(ish):

```f#
module TaskSeq =
  val toList: t: taskSeq<'T> -> 'T list
  val toArray: taskSeq: taskSeq<'T> -> 'T[]
  val empty<'T> : IAsyncEnumerable<'T>
  val ofArray: array: 'T[] -> IAsyncEnumerable<'T>
  val ofList: list: 'T list -> IAsyncEnumerable<'T>
  val ofSeq: sequence: seq<'T> -> IAsyncEnumerable<'T>
  val ofResizeArray: data: ResizeArray<'T> -> IAsyncEnumerable<'T>
  val ofTaskSeq: sequence: seq<#Task<'T>> -> IAsyncEnumerable<'T>
  val ofTaskList: list: #Task<'T> list -> IAsyncEnumerable<'T>
  val ofTaskArray: array: #Task<'T> array -> IAsyncEnumerable<'T>
  val ofAsyncSeq: sequence: seq<Async<'T>> -> IAsyncEnumerable<'T>
  val ofAsyncList: list: Async<'T> list -> IAsyncEnumerable<'T>
  val ofAsyncArray: array: Async<'T> array -> IAsyncEnumerable<'T>
  val toArrayAsync: taskSeq: taskSeq<'a> -> Task<'a[]>
  val toListAsync: taskSeq: taskSeq<'a> -> Task<'a list>
  val toResizeArrayAsync: taskSeq: taskSeq<'a> -> Task<ResizeArray<'a>>
  val toIListAsync: taskSeq: taskSeq<'a> -> Task<IList<'a>>
  val toSeqCachedAsync: taskSeq: taskSeq<'a> -> Task<seq<'a>>
  val iter: action: ('a -> unit) -> taskSeq: taskSeq<'a> -> Task<unit>
  val iteri: action: (int -> 'a -> unit) -> taskSeq: taskSeq<'a> -> Task<unit>
  val iterAsync: action: ('a -> #Task<unit>) -> taskSeq: taskSeq<'a> -> Task<unit>
  val iteriAsync: action: (int -> 'a -> #Task<unit>) -> taskSeq: taskSeq<'a> -> Task<unit>
  val map: mapper: ('T -> 'U) -> taskSeq: taskSeq<'T> -> IAsyncEnumerable<'U>
  val mapi: mapper: (int -> 'T -> 'U) -> taskSeq: taskSeq<'T> -> IAsyncEnumerable<'U>
  val mapAsync: mapper: ('a -> #Task<'c>) -> taskSeq: taskSeq<'a> -> IAsyncEnumerable<'c>
  val mapiAsync: mapper: (int -> 'a -> #Task<'c>) -> taskSeq: taskSeq<'a> -> IAsyncEnumerable<'c>
  val collect: binder: ('T -> #IAsyncEnumerable<'U>) -> taskSeq: taskSeq<'T> -> IAsyncEnumerable<'U>
  val collectSeq: binder: ('T -> #seq<'U>) -> taskSeq: taskSeq<'T> -> IAsyncEnumerable<'U>
  val collectAsync: binder: ('T -> #Task<'b>) -> taskSeq: taskSeq<'T> -> taskSeq<'U> when 'b :> IAsyncEnumerable<'U>
  val collectSeqAsync: binder: ('T -> #Task<'b>) -> taskSeq: taskSeq<'T> -> taskSeq<'U> when 'b :> seq<'U>
  val zip: taskSeq1: taskSeq<'a> -> taskSeq2: taskSeq<'b> -> IAsyncEnumerable<'a * 'b>
  val fold: folder: ('State -> 'T -> 'State) -> state: 'State -> taskSeq: IAsyncEnumerable<'T> -> Task<'State>
  val foldAsync: folder: ('State -> 'T -> #Task<'State>) -> state: 'State -> taskSeq: IAsyncEnumerable<'T> -> Task<'State>
```
