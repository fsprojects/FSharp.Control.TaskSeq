# TaskSeq<!-- omit in toc -->

An implementation of [`IAsyncEnumerable<'T>`][3] as a computation expression: `taskSeq { ... }` with an accompanying `TaskSeq` module.

-----------------------------------------

## Table of contents<!-- omit in toc -->

<!--
    This index can be auto-generated with VS Code's Markdown All in One extension.
    The ToC will be updated-on-save, or can be generated on command by using
    Ctrl-Shift-P: "Create table of contents".
    More info: https://marketplace.visualstudio.com/items?itemName=yzhang.markdown-all-in-one#table-of-contents
-->

- [Overview](#overview)
  - [Module functions](#module-functions)
  - [`taskSeq` computation expressions](#taskseq-computation-expressions)
  - [Examples](#examples)
  - [`TaskSeq` module functions](#taskseq-module-functions)
- [More information](#more-information)
  - [Further reading `IAsyncEnumerable`](#further-reading-iasyncenumerable)
  - [Further reading on resumable state machines](#further-reading-on-resumable-state-machines)
  - [Further reading on computation expressions](#further-reading-on-computation-expressions)

-----------------------------------------

## Overview

The `IAsyncEnumerable` interface was added to .NET in `.NET Core 3.0` and is part of `.NET Standard 2.1`. The main use-case was for iterative asynchronous enumeration over some resource. For instance, an event stream or a REST API interface with pagination, asynchronous reading over a list of files and accumulating the results, where each action can be modeled as a [`MoveNextAsync`][4] call on the [`IAsyncEnumerator<'T>`][5] given by a call to [`GetAsyncEnumerator()`][6].

Since the introduction of `task` in F# the call for a native implementation of _task sequences_ has grown, in particular because proper iterating over an `IAsyncEnumerable` has proven challenging, especially if one wants to avoid mutable variables. This library is an answer to that call and implements the same _resumable state machine_ approach with `taskSeq`.

### Module functions

As with `seq` and `Seq`, this library comes with a bunch of well-known collection functions, like `TaskSeq.empty`, `isEmpty` or `TaskSeq.map`, `iter`, `collect`, `fold` and `TaskSeq.find`, `pick`, `choose`, `filter`. Where applicable, these come with async variants, like `TaskSeq.mapAsync` `iterAsync`, `collectAsync`, `foldAsync` and `TaskSeq.findAsync`, `pickAsync`, `chooseAsync`, `filterAsync`, which allows the applied function to be asynchronous.

[See below](#taskseq-module-functions) for a full list of currently implemented functions and their variants.

### `taskSeq` computation expressions

The `taskSeq` computation expression can be used just like using `seq`. On top of that, it adds support for working with tasks through `let!` and
looping over a normal or asynchronous sequence (one that implements `IAsyncEnumerable<'T>'`). You can use `yield!` and `yield` and there's support
for `use` and `use!`, `try-with` and `try-finally` and `while` loops within the task sequence expression.

### Examples

```f#
open System.IO
open FSharp.Control

// singleton is fine
let helloTs = taskSeq { yield "Hello, World!" }

// cold-started, that is, delay-executed
let f() = task {
    // using toList forces execution of whole sequence
    let! hello = TaskSeq.toList helloTs  // toList returns a Task<'T list>
    return List.head hello 
}

// can be mixed with normal sequences
let oneToTen = taskSeq { yield! [1..10] }

// can be used with F#'s task and async in a for-loop
let f() = task { for x in oneToTen do printfn "Number %i" x }
let g() = async { for x in oneToTen do printfn "Number %i" x }

// returns a delayed sequence of IAsyncEnumerable<string>
let allFilesAsLines() = taskSeq {
    let files = Directory.EnumerateFiles(@"c:\temp")
    for file in files do
        // await
        let! contents = File.ReadAllLinesAsync file
        // return all lines
        yield! contents
}

let write file = 
    allFilesAsLines()
    
    // synchronous map function on asynchronous task sequence
    |> TaskSeq.map (fun x -> x.Replace("a", "b"))

    // asynchronous map
    |> TaskSeq.mapAsync (fun x -> task { return "hello: " + x })
    
    // asynchronous iter
    |> TaskSeq.iterAsync (fun data -> File.WriteAllTextAsync(fileName, data))


// infinite sequence
let feedFromTwitter user pwd = taskSeq {
    do! loginToTwitterAsync(user, pwd)
    while true do
       let! message = getNextNextTwitterMessageAsync()
       yield message
}
```

### `TaskSeq` module functions

We are working hard on getting a full set of module functions on `TaskSeq` that can be used with `IAsyncEnumerable` sequences. Our guide is the set of F# `Seq` functions in F# Core and, where applicable, the functions provided from `AsyncSeq`. Each implemented function is documented through XML doc comments to provide the necessary context-sensitive help.

The following is the progress report:

| Done             | `Seq`              | `TaskSeq`       | Variants             | Remarks                                                                                                                                                                                                                                                                                                                |
|------------------|--------------------|-----------------|----------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| &#x2753;         | `allPairs`         | `allPairs`      |                      | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2705; [#81][] | `append`           | `append`        |                      | |
| &#x2705; [#81][] |                    |                 | `appendSeq`          | |
| &#x2705; [#81][] |                    |                 | `prependSeq`         | |
|                  | `average`          | `average`       |                      | |
|                  | `averageBy`        | `averageBy`     | `averageByAsync`     | |
| &#x2753;         | `cache`            | `cache`         |                      | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2705; [#67][] | `cast`             | `cast`          |                      | |
| &#x2705; [#67][] |                    |                 | `box`                | |
| &#x2705; [#67][] |                    |                 | `unbox`              | |
| &#x2705; [#23][] | `choose`           | `choose`        | `chooseAsync`        | |
|                  | `chunkBySize`      | `chunkBySize`   |                      | |
| &#x2705; [#11][] | `collect`          | `collect`       | `collectAsync`       | |
| &#x2705; [#11][] |                    | `collectSeq`    | `collectSeqAsync`    | |
|                  | `compareWith`      | `compareWith`   | `compareWithAsync`   | |
| &#x2705; [#69][] | `concat`           | `concat`        |                      | |
| &#x2705; [#70][] | `contains`         | `contains`      |                      | |
| &#x2705; [#82][] | `delay`            | `delay`         |                      | |
|                  | `distinct`         | `distinct`      |                      | |
|                  | `distinctBy`       | `dictinctBy`    | `distinctByAsync`    | |
| &#x2705; [#2][]  | `empty`            | `empty`         |                      | |
| &#x2705; [#23][] | `exactlyOne`       | `exactlyOne`    |                      | |
| &#x2705; [#83][] | `except`           | `except`        |                      | |
| &#x2705; [#83][] |                    | `exceptOfSeq`   |                      | |
| &#x2705; [#70][] | `exists`           | `exists`        | `existsAsync`        | |
|                  | `exists2`          | `exists2`       |                      | |
| &#x2705; [#23][] | `filter`           | `filter`        | `filterAsync`        | |
| &#x2705; [#23][] | `find`             | `find`          | `findAsync`          | |
| &#x1f6ab;        | `findBack`         |                 |                      | [note #2](#note-2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#68][] | `findIndex`        | `findIndex`     | `findIndexAsync`     | |
| &#x1f6ab;        | `findIndexBack`    | n/a             | n/a                  | [note #2](#note-2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#2][]  | `fold`             | `fold`          | `foldAsync`          | |
|                  | `fold2`            | `fold2`         | `fold2Async`         | |
| &#x1f6ab;        | `foldBack`         |                 |                      | [note #2](#note-2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x1f6ab;        | `foldBack2`        |                 |                      | [note #2](#note-2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
|                  | `forall`           | `forall`        | `forallAsync`        | |
|                  | `forall2`          | `forall2`       | `forall2Async`       | |
| &#x2753;         | `groupBy`          | `groupBy`       | `groupByAsync`       | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2705; [#23][] | `head`             | `head`          |                      | |
| &#x2705; [#68][] | `indexed`          | `indexed`       |                      | |
| &#x2705; [#69][] | `init`             | `init`          | `initAsync`          | |
| &#x2705; [#69][] | `initInfinite`     | `initInfinite`  | `initInfiniteAsync`  | |
|                  | `insertAt`         | `insertAt`      |                      | |
|                  | `insertManyAt`     | `insertManyAt`  |                      | |
| &#x2705; [#23][] | `isEmpty`          | `isEmpty`       |                      | |
| &#x2705; [#23][] | `item`             | `item`          |                      | |
| &#x2705; [#2][]  | `iter`             | `iter`          | `iterAsync`          | |
|                  | `iter2`            | `iter2`         | `iter2Async`         | |
| &#x2705; [#2][]  | `iteri`            | `iteri`         | `iteriAsync`         | |
|                  | `iteri2`           | `iteri2`        | `iteri2Async`        | |
| &#x2705; [#23][] | `last`             | `last`          |                      | |
| &#x2705; [#53][] | `length`           | `length`        |                      | |
| &#x2705; [#53][] |                    | `lengthBy`      | `lengthByAsync`      | |
| &#x2705; [#2][]  | `map`              | `map`           | `mapAsync`           | |
|                  | `map2`             | `map2`          | `map2Async`          | |
|                  | `map3`             | `map3`          | `map3Async`          | |
|                  | `mapFold`          | `mapFold`       | `mapFoldAsync`       | |
| &#x1f6ab;        | `mapFoldBack`      |                 |                      | [note #2](#note-2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#2][]  | `mapi`             | `mapi`          | `mapiAsync`          | |
|                  | `mapi2`            | `mapi2`         | `mapi2Async`         | |
|                  | `max`              | `max`           |                      | |
|                  | `maxBy`            | `maxBy`         | `maxByAsync`         | |
|                  | `min`              | `min`           |                      | |
|                  | `minBy`            | `minBy`         | `minByAsync`         | |
| &#x2705; [#2][]  | `ofArray`          | `ofArray`       |                      | |
| &#x2705; [#2][]  |                    | `ofAsyncArray`  |                      | |
| &#x2705; [#2][]  |                    | `ofAsyncList`   |                      | |
| &#x2705; [#2][]  |                    | `ofAsyncSeq`    |                      | |
| &#x2705; [#2][]  | `ofList`           | `ofList`        |                      | |
| &#x2705; [#2][]  |                    | `ofTaskList`    |                      | |
| &#x2705; [#2][]  |                    | `ofResizeArray` |                      | |
| &#x2705; [#2][]  |                    | `ofSeq`         |                      | |
| &#x2705; [#2][]  |                    | `ofTaskArray`   |                      | |
| &#x2705; [#2][]  |                    | `ofTaskList`    |                      | |
| &#x2705; [#2][]  |                    | `ofTaskSeq`     |                      | |
|                  | `pairwise`         | `pairwise`      |                      | |
|                  | `permute`          | `permute`       | `permuteAsync`       | |
| &#x2705; [#23][] | `pick`             | `pick`          | `pickAsync`          | |
| &#x1f6ab;        | `readOnly`         |                 |                      | [note #3](#note-3 "The motivation for 'readOnly' in 'Seq' is that a cast from a mutable array or list to a 'seq<_>' is valid and can be cast back, leading to a mutable sequence. Since 'TaskSeq' doesn't implement 'IEnumerable<_>', such casts are not possible.") |
|                  | `reduce`           | `reduce`        | `reduceAsync`        | |
| &#x1f6ab;        | `reduceBack`       |                 |                      | [note #2](#note-2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
|                  | `removeAt`         | `removeAt`      |                      | |
|                  | `removeManyAt`     | `removeManyAt`  |                      | |
|                  | `replicate`        | `replicate`     |                      | |
| &#x2753;         | `rev`              |                 |                      | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
|                  | `scan`             | `scan`          | `scanAsync`          | |
| &#x1f6ab;        | `scanBack`         |                 |                      | [note #2](#note-2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#90][] | `singleton`        | `singleton`     |                      | |
|                  | `skip`             | `skip`          |                      | |
|                  | `skipWhile`        | `skipWhile`     | `skipWhileAsync`     | |
| &#x2753;         | `sort`             |                 |                      | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2753;         | `sortBy`           |                 |                      | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2753;         | `sortByAscending`  |                 |                      | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2753;         | `sortByDescending` |                 |                      | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2753;         | `sortWith`         |                 |                      | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
|                  | `splitInto`        | `splitInto`     |                      | |
|                  | `sum`              | `sum`           |                      | |
|                  | `sumBy`            | `sumBy`         | `sumByAsync`         | |
| &#x2705; [#76][] | `tail`             | `tail`          |                      | |
|                  | `take`             | `take`          |                      | |
|                  | `takeWhile`        | `takeWhile`     | `takeWhileAsync`     | |
| &#x2705; [#2][]  | `toArray`          | `toArray`       | `toArrayAsync`       | |
| &#x2705; [#2][]  |                    | `toIList`       | `toIListAsync`       | |
| &#x2705; [#2][]  | `toList`           | `toList`        | `toListAsync`        | |
| &#x2705; [#2][]  |                    | `toResizeArray` | `toResizeArrayAsync` | |
| &#x2705; [#2][]  |                    | `toSeq`         | `toSeqAsync`         | |
|                  |                    | […]             |                      | |
| &#x2753;         | `transpose`        |                 |                      | [note #1](#note-1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
|                  | `truncate`         | `truncate`      |                      | |
| &#x2705; [#23][] | `tryExactlyOne`    | `tryExactlyOne` | `tryExactlyOneAsync` | |
| &#x2705; [#23][] | `tryFind`          | `tryFind`       | `tryFindAsync`       | |
| &#x1f6ab;        | `tryFindBack`      |                 |                      | [note #2](#note-2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#68][] | `tryFindIndex`     | `tryFindIndex`  | `tryFindIndexAsync`  | |
| &#x1f6ab;        | `tryFindIndexBack` |                 |                      | [note #2](#note-2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#23][] | `tryHead`          | `tryHead`       |                      | |
| &#x2705; [#23][] | `tryItem`          | `tryItem`       |                      | |
| &#x2705; [#23][] | `tryLast`          | `tryLast`       |                      | |
| &#x2705; [#23][] | `tryPick`          | `tryPick`       | `tryPickAsync`       | |
| &#x2705; [#76][] |                    | `tryTail`       |                      | |
|                  | `unfold`           | `unfold`        | `unfoldAsync`        | |
|                  | `updateAt`         | `updateAt`      |                      | |
|                  | `where`            | `where`         | `whereAsync`         | |
|                  | `windowed`         | `windowed`      |                      | |
| &#x2705; [#2][]  | `zip`              | `zip`           |                      | |
|                  | `zip3`             | `zip3`          |                      | |
|                  |                    | `zip4`          |                      | |

#### Note 1<!-- omit in toc -->

_These functions require a form of pre-materializing through `TaskSeq.cache`, similar to the approach taken in the corresponding `Seq` functions. It doesn't make much sense to have a cached async sequence. However, `AsyncSeq` does implement these, so we'll probably do so eventually as well._  

#### Note 2<!-- omit in toc -->

_Because of the async nature of `TaskSeq` sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the `xxxBack` iterators._  

#### Note 3<!-- omit in toc -->

_The motivation for `readOnly` in `Seq` is that a cast from a mutable array or list to a `seq<_>` is valid and can be cast back, leading to a mutable sequence. Since `TaskSeq` doesn't implement `IEnumerable<_>`, such casts are not possible._

## More information

### Further reading `IAsyncEnumerable`

- A good C#-based introduction [can be found in this blog][8].
- [An MSDN article][9] written shortly after it was introduced.
- Converting a `seq` to an `IAsyncEnumerable` [demo gist][10] as an example, though `TaskSeq` contains many more utility functions and uses a slightly different approach.
- If you're looking for using `IAsyncEnumerable` with `async` and not `task`, the excellent [`AsyncSeq`][11] library should be used. While `TaskSeq` is intended to consume `async` just like `task` does, it won't create an `AsyncSeq` type (at least not yet). If you want classic Async and parallelism, you should get this library instead.

### Further reading on resumable state machines

- A state machine from a monadic perspective in F# [can be found here][12], which works with the pre-F# 6.0 non-resumable internals.
- The [original RFC for F# 6.0 on resumable state machines][13]
- The [original RFC for introducing `task`][14] to F# 6.0.
- A [pre F# 6.0 `TaskBuilder`][15] that motivated the `task` CE later added to F# Core.
- [MSDN Documentation on `task`][16] and [`async`][17].

### Further reading on computation expressions

- [Docs on MSDN][18] form a good summary and starting point.
- Arguably the best [step-by-step tutorial to using and building computation expressions][19] by Scott Wlaschin.

[3]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=net-7.0
[4]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerator-1.movenextasync?view=net-7.0
[5]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerator-1?view=net-7.0
[6]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1.getasyncenumerator?view=net-7.0
[7]: https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable?view=net-7.0
[8]: https://stu.dev/iasyncenumerable-introduction/
[9]: https://learn.microsoft.com/en-us/archive/msdn-magazine/2019/november/csharp-iterating-with-async-enumerables-in-csharp-8
[10]: https://gist.github.com/akhansari/d88812b742aa6be1c35b4f46bd9f8532
[11]: https://fsprojects.github.io/FSharp.Control.AsyncSeq/AsyncSeq.html
[12]: http://blumu.github.io/ResumableMonad/TheResumableMonad.html
[13]: https://github.com/fsharp/fslang-design/blob/main/FSharp-6.0/FS-1087-resumable-code.md
[14]: https://github.com/fsharp/fslang-design/blob/main/FSharp-6.0/FS-1097-task-builder.md
[15]: https://github.com/rspeele/TaskBuilder.fs/
[16]: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions
[17]: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions
[18]: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions
[19]: https://fsharpforfunandprofit.com/series/computation-expressions/

[#2]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/2
[#11]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/11
[#23]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/23
[#53]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/53
[#67]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/67
[#68]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/68
[#69]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/69
[#70]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/70
[#76]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/76
[#81]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/81
[#82]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/82
[#83]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/83