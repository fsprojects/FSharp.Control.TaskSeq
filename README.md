[![build][buildstatus_img]][buildstatus]
[![test][teststatus_img]][teststatus]
[![Nuget](https://buildstats.info/nuget/FSharp.Control.TaskSeq?includePreReleases=true)](https://www.nuget.org/packages/FSharp.Control.TaskSeq/)

# TaskSeq<!-- omit in toc -->

An implementation of [`IAsyncEnumerable<'T>`][3] as a computation expression: `taskSeq { ... }` with an accompanying `TaskSeq` module and functions, that allow seamless use of asynchronous sequences similar to F#'s native `seq` and `task` CE's.

* Latest stable version: [0.4.0 is on NuGet][nuget].

## Release notes<!-- omit in toc -->

See [Releases](https://github.com/fsprojects/FSharp.Control.TaskSeq/releases) for the an extensive version history of `TaskSeq`. See [Status overview](#status--planning) below for a progress report.

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
  - [Installation](#installation)
  - [Examples](#examples)
- [Choosing between `AsyncSeq` and `TaskSeq`](#choosing-between-asyncseq-and-taskseq)
- [Status \& planning](#status--planning)
  - [Implementation progress](#implementation-progress)
  - [Progress `taskSeq` CE](#progress-taskseq-ce)
  - [Progress and implemented `TaskSeq` module functions](#progress-and-implemented-taskseq-module-functions)
- [More information](#more-information)
  - [Further reading on `IAsyncEnumerable`](#further-reading-on-iasyncenumerable)
  - [Further reading on resumable state machines](#further-reading-on-resumable-state-machines)
  - [Further reading on computation expressions](#further-reading-on-computation-expressions)
- [Building \& testing](#building--testing)
  - [Prerequisites](#prerequisites)
  - [Build the solution](#build-the-solution)
  - [Run the tests](#run-the-tests)
  - [Run the CI command](#run-the-ci-command)
  - [Advanced](#advanced)
  - [Get help](#get-help)
- [Work in progress](#work-in-progress)
- [Current set of `TaskSeq` utility functions](#current-set-of-taskseq-utility-functions)

-----------------------------------------

## Overview

The `IAsyncEnumerable` interface was added to .NET in `.NET Core 3.0` and is part of `.NET Standard 2.1`. The main use-case was for iterative asynchronous, sequential enumeration over some resource. For instance, an event stream or a REST API interface with pagination, asynchronous reading over a list of files and accumulating the results, where each action can be modeled as a [`MoveNextAsync`][4] call on the [`IAsyncEnumerator<'T>`][3] given by a call to [`GetAsyncEnumerator()`][6].

Since the introduction of `task` in F# the call for a native implementation of _task sequences_ has grown, in particular because proper iteration over an `IAsyncEnumerable` has proven challenging, especially if one wants to avoid mutable variables. This library is an answer to that call and applies the same _resumable state machine_ approach with `taskSeq`.

### Module functions

As with `seq` and `Seq`, this library comes with a bunch of well-known collection functions, like `TaskSeq.empty`, `isEmpty` or `TaskSeq.map`, `iter`, `collect`, `fold` and `TaskSeq.find`, `pick`, `choose`, `filter`, `takeWhile`. Where applicable, these come with async variants, like `TaskSeq.mapAsync` `iterAsync`, `collectAsync`, `foldAsync` and `TaskSeq.findAsync`, `pickAsync`, `chooseAsync`, `filterAsync`, `takeWhileAsync` which allows the applied function to be asynchronous.

[See below](#current-set-of-taskseq-utility-functions) for a full list of currently implemented functions and their variants.

### `taskSeq` computation expressions

The `taskSeq` computation expression can be used just like using `seq`.
Additionally, it adds support for working with `Task`s through `let!` and
looping over both normal and asynchronous sequences (ones that implement
`IAsyncEnumerable<'T>'`). You can use `yield!` and `yield` and there's support
for `use` and `use!`, `try-with` and `try-finally` and `while` loops within
the task sequence expression:

### Installation

Dotnet Nuget

```cmd
dotnet add package FSharp.Control.TaskSeq
```

For a specific project:

```cmd
dotnet add myproject.fsproj package FSharp.Control.TaskSeq
```

F# Interactive (FSI):

```f#
// latest version
> #r "nuget: FSharp.Control.TaskSeq"

// or with specific version
> #r "nuget: FSharp.Control.TaskSeq, 0.4.0"
```

Paket:

```cmd
dotnet paket add FSharp.Control.TaskSeq --project <project>
```

Package Manager:

```cmd
PM> NuGet\Install-Package FSharp.Control.TaskSeq
```

As package reference in `fsproj` or `csproj` file:

```xml
<!-- replace version with most recent version -->
<PackageReference Include="FSharp.Control.TaskSeq" Version="0.4.0" />
```

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

## Choosing between `AsyncSeq` and `TaskSeq`

The [`AsyncSeq`][11] and `TaskSeq` libraries both operate on asynchronous sequences, but there are a few fundamental differences. The most notable being that the former _does not_ implement `IAsyncEnumerable<'T>`, though it does have a type of that name with different semantics (not surprising; it predates the definition of the modern one). Another key difference is that `TaskSeq` uses `ValueTask`s for the asynchronous computations, whereas `AsyncSeq` uses F#'s `Async<'T>`.

There are more differences:

|                            | `TaskSeq`                                                                       | `AsyncSeq`                                                           |
|----------------------------|---------------------------------------------------------------------------------|----------------------------------------------------------------------|
| **Frameworks**             | .NET 5.0+, NetStandard 2.1                                                      | .NET 5.0+, NetStandard 2.0 and 2.1, .NET Framework 4.6.1+            |
| **F# concept of**          | `task`                                                                          | `async`                                                              |
| **Underlying type**        | [`Generic.IAsyncEnumerable<'T>`][3] <sup>[note #1](#tsnote1 "Full name System.Collections.Generic.IAsyncEnumerable&lt;'T>.")</sup>| Its own type, also called `IAsyncEnumerable<'T>`<sup>[note #1](#tsnote1 "Full name FSharp.Control.IAsyncEnumerable&lt;'T>.")</sup> |
| **Implementation**         | State machine (statically compiled)                                             | No state machine, continuation style                                 |
| **Semantics**              | `seq`-like: on-demand                                                           | `seq`-like: on-demand                                                |
| **Disposability**          | Asynchronous, through [`IAsyncDisposable`][7]                                   | Synchronous, through `IDisposable`                                   |
| **Support `let!`**         | All `task`-like: `Async<'T>`, `Task<'T>`, `ValueTask<'T>` or any `GetAwaiter()` | `Async<'T>` only                                                     |
| **Support `do!`**          | `Async<unit>`, `Task<unit>` and `Task`, `ValueTask<unit>` and `ValueTask`       | `Async<unit>` only                                                   |
| **Support `yield!`**       | [`IAsyncEnumerable<'T>`][3] (= `TaskSeq`), `AsyncSeq`, any sequence             | `AsyncSeq`                                                           |
| **Support `for`**          | [`IAsyncEnumerable<'T>`][3] (= `TaskSeq`), `AsyncSeq`, any sequence             | `AsyncSeq`, any sequence                                             |
| **Behavior with `yield`**  | Zero allocations; no `Task` or even `ValueTask` created                         | Allocates an F# `Async` wrapped in a singleton `AsyncSeq`            |
| **Conversion to other**    | `TaskSeq.toAsyncSeq`                                                            | [`AsyncSeq.toAsyncEnum`][22]                                         |
| **Conversion from other**  | Implicit (`yield!`) or `TaskSeq.ofAsyncSeq`                                     | [`AsyncSeq.ofAsyncEnum`][23]                                         |
| **Recursion in `yield!`**  | **No** (requires F# support, upcoming)                                          | Yes                                                                  |
| **Iteration semantics**    | [Two operations][6], 'Next' is a value task, 'Current' must be called separately| One operation, 'Next' is `Async`, returns `option` with 'Current'    |
| **`MoveNextAsync`**        | [Returns `ValueTask<bool>`][4]                                                  | Returns `Async<'T option>`                                           |
| **[`Current`][5]**         | [Returns `'T`][5]                                                               | n/a                                                                  |
| **Cancellation**           | See [#133][], until 0.3.0: use `GetAsyncEnumerator(cancelToken)`                | Implicit token flows to all subtasks per `async` semantics           |
| **Performance**            | Very high, negligible allocations                                               | Slower, more allocations, due to using `async` and cont style        |
| **Parallelism**            | Unclear, interface is meant for _sequential/async_ processing                   | Supported by extension functions                                     |

<sup>¹⁾ <a id="tsnote1"></a>_Both `AsyncSeq` and `TaskSeq` use a type called `IAsyncEnumerable<'T>`, but only `TaskSeq` uses the type from the BCL Generic Collections. `AsyncSeq` supports .NET Framework 4.6.x and NetStandard 2.0 as well, which do not have this type in the BCL._</sup>

## Status & planning

The `TaskSeq` project already has a wide array of functions and functionalities, see overview below. The current status is: *STABLE*. However, certain features we'd really like to add:

- [x] Take existing `taskSeq` resumable code from F# and fix it. **DONE**
- [x] Add almost all functions from `Seq` that could apply to `TaskSeq` (full overview below). **MOSTLY DONE, STILL TODO**
- [ ] Add remaining relevant functions from `Seq`. **PLANNED FOR 0.4.x**
  - [x] `min` / `max` / `minBy` / `maxBy` & async variant (see [#221])
  - [x] `insertAt` / `updateAt` and related (see [#236])
  - [ ] `average` / `averageBy`, `sum` and related
  - [x] `forall` / `forallAsync` (see [#240])
  - [x] `skip` / `drop` / `truncate` / `take` (see [#209])
  - [ ] `chunkBySize` / `windowed`
  - [ ] `compareWith`
  - [ ] `distinct`
  - [ ] `exists2` / `map2` / `fold2` / `iter2` and related '2'-functions
  - [ ] `mapFold`
  - [ ] `pairwise` / `allpairs` / `permute` / `distinct` / `distinctBy`
  - [ ] `replicate`
  - [ ] `reduce` / `scan`
  - [ ] `unfold`
- [x] Publish package on Nuget, **DONE, PUBLISHED SINCE: 7 November 2022**. See https://www.nuget.org/packages/FSharp.Control.TaskSeq
- [x] Make `TaskSeq` interoperable with `Task` by expanding the latter with a `for .. in .. do` that acceps task sequences
- [x] Add to/from functions to seq, list, array
- [ ] Add applicable functions from `AsyncSeq`. **PLANNED FOR 0.5-alpha**
- [ ] (Better) support for cancellations
  - [ ] Make the tasks cancellable with token (see [#133]). **PLANNED FOR 0.5-alpha**
  - [ ] Support `ConfiguredCancelableAsyncEnumerable` (see [#167]). **PLANNED FOR 0.5-alpha**
  - [ ] Interop with `cancellableTask` and `valueTask` from [`IcedTasks`][24]
- [ ] Interop with `AsyncSeq`.
- [ ] (maybe) Support any awaitable type in the function lib (that is: where a `Task` is required, accept a `ValueTask` and `Async` as well)
- [ ] Add `TaskEx` functionality (separate lib). **DISCUSSION**
- [ ] Move documentation to <https://fsprojects.github.io>

### Implementation progress

 * As of 9 November 2022: [Nuget package available][21]. In this phase, we will frequently update the package, see [release notes.txt](release-notes.txt). Current version:
 * Major update: 17 March 2024, version 0.4.0

[![Nuget](https://img.shields.io/nuget/vpre/FSharp.Control.TaskSeq)](https://www.nuget.org/packages/FSharp.Control.TaskSeq/)

### Progress `taskSeq` CE

The _resumable state machine_ backing the `taskSeq` CE is now finished and _restartability_ (not to be confused with _resumability_) has been implemented and stabilized. Full support for empty task sequences is done. Focus is now on adding functionality there, like adding more useful overloads for `yield` and `let!`. [Suggestions are welcome!][issues].

### Progress and implemented `TaskSeq` module functions

We are working hard on getting a full set of module functions on `TaskSeq` that can be used with `IAsyncEnumerable` sequences. Our guide is the set of F# `Seq` functions in F# Core and, where applicable, the functions provided by `AsyncSeq`. Each implemented function is documented through XML doc comments to provide the necessary context-sensitive help.

This is what has been implemented so far, is planned or skipped:

| Done             | `Seq`              | `TaskSeq`            | Variants                  | Remarks                                                                                                                                                                                                                                                                                                                |
|------------------|--------------------|----------------------|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| &#x2753;         | `allPairs`         | `allPairs`           |                           | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2705; [#81][] | `append`           | `append`             |                           | |
| &#x2705; [#81][] |                    |                      | `appendSeq`               | |
| &#x2705; [#81][] |                    |                      | `prependSeq`              | |
|                  | `average`          | `average`            |                           | |
|                  | `averageBy`        | `averageBy`          | `averageByAsync`          | |
| &#x2753;         | `cache`            | `cache`              |                           | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2705; [#67][] | `cast`             | `cast`               |                           | |
| &#x2705; [#67][] |                    |                      | `box`                     | |
| &#x2705; [#67][] |                    |                      | `unbox`                   | |
| &#x2705; [#23][] | `choose`           | `choose`             | `chooseAsync`             | |
|                  | `chunkBySize`      | `chunkBySize`        |                           | |
| &#x2705; [#11][] | `collect`          | `collect`            | `collectAsync`            | |
| &#x2705; [#11][] |                    | `collectSeq`         | `collectSeqAsync`         | |
|                  | `compareWith`      | `compareWith`        | `compareWithAsync`        | |
| &#x2705; [#69][] | `concat`           | `concat`             |                           | |
| &#x2705; [#237][]| `concat` (list)    | `concat` (list)      |                           | |
| &#x2705; [#237][]| `concat` (array)   | `concat` (array)     |                           | |
| &#x2705; [#237][]| `concat` (r-array) | `concat` (r-array)   |                           | |
| &#x2705; [#237][]| `concat` (seq)     | `concat` (seq)       |                           | |
| &#x2705; [#70][] | `contains`         | `contains`           |                           | |
| &#x2705; [#82][] | `delay`            | `delay`              |                           | |
|                  | `distinct`         | `distinct`           |                           | |
|                  | `distinctBy`       | `dictinctBy`         | `distinctByAsync`         | |
| &#x2705; [#209][]|                    | `drop`               |                           | |
| &#x2705; [#2][]  | `empty`            | `empty`              |                           | |
| &#x2705; [#23][] | `exactlyOne`       | `exactlyOne`         |                           | |
| &#x2705; [#83][] | `except`           | `except`             |                           | |
| &#x2705; [#83][] |                    | `exceptOfSeq`        |                           | |
| &#x2705; [#70][] | `exists`           | `exists`             | `existsAsync`             | |
|                  | `exists2`          | `exists2`            |                           | |
| &#x2705; [#23][] | `filter`           | `filter`             | `filterAsync`             | |
| &#x2705; [#23][] | `find`             | `find`               | `findAsync`               | |
| &#x1f6ab;        | `findBack`         |                      |                           | [note #2](#note2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#68][] | `findIndex`        | `findIndex`          | `findIndexAsync`          | |
| &#x1f6ab;        | `findIndexBack`    | n/a                  | n/a                       | [note #2](#note2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#2][]  | `fold`             | `fold`               | `foldAsync`               | |
|                  | `fold2`            | `fold2`              | `fold2Async`              | |
| &#x1f6ab;        | `foldBack`         |                      |                           | [note #2](#note2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x1f6ab;        | `foldBack2`        |                      |                           | [note #2](#note2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#240][]| `forall`           | `forall`             | `forallAsync`             | |
|                  | `forall2`          | `forall2`            | `forall2Async`            | |
| &#x2753;         | `groupBy`          | `groupBy`            | `groupByAsync`            | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2705; [#23][] | `head`             | `head`               |                           | |
| &#x2705; [#68][] | `indexed`          | `indexed`            |                           | |
| &#x2705; [#69][] | `init`             | `init`               | `initAsync`               | |
| &#x2705; [#69][] | `initInfinite`     | `initInfinite`       | `initInfiniteAsync`       | |
| &#x2705; [#236][]| `insertAt`         | `insertAt`           |                           | |
| &#x2705; [#236][]| `insertManyAt`     | `insertManyAt`       |                           | |
| &#x2705; [#23][] | `isEmpty`          | `isEmpty`            |                           | |
| &#x2705; [#23][] | `item`             | `item`               |                           | |
| &#x2705; [#2][]  | `iter`             | `iter`               | `iterAsync`               | |
|                  | `iter2`            | `iter2`              | `iter2Async`              | |
| &#x2705; [#2][]  | `iteri`            | `iteri`              | `iteriAsync`              | |
|                  | `iteri2`           | `iteri2`             | `iteri2Async`             | |
| &#x2705; [#23][] | `last`             | `last`               |                           | |
| &#x2705; [#53][] | `length`           | `length`             |                           | |
| &#x2705; [#53][] |                    | `lengthBy`           | `lengthByAsync`           | |
| &#x2705; [#2][]  | `map`              | `map`                | `mapAsync`                | |
|                  | `map2`             | `map2`               | `map2Async`               | |
|                  | `map3`             | `map3`               | `map3Async`               | |
|                  | `mapFold`          | `mapFold`            | `mapFoldAsync`            | |
| &#x1f6ab;        | `mapFoldBack`      |                      |                           | [note #2](#note2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#2][]  | `mapi`             | `mapi`               | `mapiAsync`               | |
|                  | `mapi2`            | `mapi2`              | `mapi2Async`              | |
| &#x2705; [#221][]| `max`              | `max`                |                           | |
| &#x2705; [#221][]| `maxBy`            | `maxBy`              | `maxByAsync`              | |
| &#x2705; [#221][]| `min`              | `min`                |                           | |
| &#x2705; [#221][]| `minBy`            | `minBy`              | `minByAsync`              | |
| &#x2705; [#2][]  | `ofArray`          | `ofArray`            |                           | |
| &#x2705; [#2][]  |                    | `ofAsyncArray`       |                           | |
| &#x2705; [#2][]  |                    | `ofAsyncList`        |                           | |
| &#x2705; [#2][]  |                    | `ofAsyncSeq`         |                           | |
| &#x2705; [#2][]  | `ofList`           | `ofList`             |                           | |
| &#x2705; [#2][]  |                    | `ofTaskList`         |                           | |
| &#x2705; [#2][]  |                    | `ofResizeArray`      |                           | |
| &#x2705; [#2][]  |                    | `ofSeq`              |                           | |
| &#x2705; [#2][]  |                    | `ofTaskArray`        |                           | |
| &#x2705; [#2][]  |                    | `ofTaskList`         |                           | |
| &#x2705; [#2][]  |                    | `ofTaskSeq`          |                           | |
|                  | `pairwise`         | `pairwise`           |                           | |
|                  | `permute`          | `permute`            | `permuteAsync`            | |
| &#x2705; [#23][] | `pick`             | `pick`               | `pickAsync`               | |
| &#x1f6ab;        | `readOnly`         |                      |                           | [note #3](#note3 "The motivation for 'readOnly' in 'Seq' is that a cast from a mutable array or list to a 'seq<_>' is valid and can be cast back, leading to a mutable sequence. Since 'TaskSeq' doesn't implement 'IEnumerable<_>', such casts are not possible.") |
|                  | `reduce`           | `reduce`             | `reduceAsync`             | |
| &#x1f6ab;        | `reduceBack`       |                      |                           | [note #2](#note2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#236][]| `removeAt`         | `removeAt`           |                           | |
| &#x2705; [#236][]| `removeManyAt`     | `removeManyAt`       |                           | |
|                  | `replicate`        | `replicate`          |                           | |
| &#x2753;         | `rev`              |                      |                           | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
|                  | `scan`             | `scan`               | `scanAsync`               | |
| &#x1f6ab;        | `scanBack`         |                      |                           | [note #2](#note2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#90][] | `singleton`        | `singleton`          |                           | |
| &#x2705; [#209][]| `skip`             | `skip`               |                           | |
| &#x2705; [#219][]| `skipWhile`        | `skipWhile`          | `skipWhileAsync`          | |
| &#x2705; [#219][]|                    | `skipWhileInclusive` | `skipWhileInclusiveAsync` | |
| &#x2753;         | `sort`             |                      |                           | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2753;         | `sortBy`           |                      |                           | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2753;         | `sortByAscending`  |                      |                           | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2753;         | `sortByDescending` |                      |                           | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2753;         | `sortWith`         |                      |                           | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
|                  | `splitInto`        | `splitInto`          |                           | |
|                  | `sum`              | `sum`                |                           | |
|                  | `sumBy`            | `sumBy`              | `sumByAsync`              | |
| &#x2705; [#76][] | `tail`             | `tail`               |                           | |
| &#x2705; [#209][]| `take`             | `take`               |                           | |
| &#x2705; [#126][]| `takeWhile`        | `takeWhile`          | `takeWhileAsync`          | |
| &#x2705; [#126][]|                    | `takeWhileInclusive` | `takeWhileInclusiveAsync` | |
| &#x2705; [#2][]  | `toArray`          | `toArray`            | `toArrayAsync`            | |
| &#x2705; [#2][]  |                    | `toIList`            | `toIListAsync`            | |
| &#x2705; [#2][]  | `toList`           | `toList`             | `toListAsync`             | |
| &#x2705; [#2][]  |                    | `toResizeArray`      | `toResizeArrayAsync`      | |
| &#x2705; [#2][]  |                    | `toSeq`              | `toSeqAsync`              | |
|                  |                    | […]                  |                           | |
| &#x2753;         | `transpose`        |                      |                           | [note #1](#note1 "These functions require a form of pre-materializing through 'TaskSeq.cache', similar to the approach taken in the corresponding 'Seq' functions. It doesn't make much sense to have a cached async sequence. However, 'AsyncSeq' does implement these, so we'll probably do so eventually as well.") |
| &#x2705; [#209][]| `truncate`         | `truncate`           |                           | |
| &#x2705; [#23][] | `tryExactlyOne`    | `tryExactlyOne`      | `tryExactlyOneAsync`      | |
| &#x2705; [#23][] | `tryFind`          | `tryFind`            | `tryFindAsync`            | |
| &#x1f6ab;        | `tryFindBack`      |                      |                           | [note #2](#note2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#68][] | `tryFindIndex`     | `tryFindIndex`       | `tryFindIndexAsync`       | |
| &#x1f6ab;        | `tryFindIndexBack` |                      |                           | [note #2](#note2 "Because of the async nature of TaskSeq sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the 'Back' iterators.") |
| &#x2705; [#23][] | `tryHead`          | `tryHead`            |                           | |
| &#x2705; [#23][] | `tryItem`          | `tryItem`            |                           | |
| &#x2705; [#23][] | `tryLast`          | `tryLast`            |                           | |
| &#x2705; [#23][] | `tryPick`          | `tryPick`            | `tryPickAsync`            | |
| &#x2705; [#76][] |                    | `tryTail`            |                           | |
|                  | `unfold`           | `unfold`             | `unfoldAsync`             | |
| &#x2705; [#236][]| `updateAt`         | `updateAt`           |                           | |
| &#x2705; [#217][]| `where`            | `where`              | `whereAsync`              | |
|                  | `windowed`         | `windowed`           |                           | |
| &#x2705; [#2][]  | `zip`              | `zip`                |                           | |
|                  | `zip3`             | `zip3`               |                           | |
|                  |                    | `zip4`               |                           | |


<sup>¹⁾ <a id="note1"></a>_These functions require a form of pre-materializing through `TaskSeq.cache`, similar to the approach taken in the corresponding `Seq` functions. It doesn't make much sense to have a cached async sequence. However, `AsyncSeq` does implement these, so we'll probably do so eventually as well._</sup>
<sup>²⁾ <a id="note2"></a>_Because of the async nature of `TaskSeq` sequences, iterating from the back would be bad practice. Instead, materialize the sequence to a list or array and then apply the `xxxBack` iterators._</sup>
<sup>³⁾ <a id="note3"></a>_The motivation for `readOnly` in `Seq` is that a cast from a mutable array or list to a `seq<_>` is valid and can be cast back, leading to a mutable sequence. Since `TaskSeq` doesn't implement `IEnumerable<_>`, such casts are not possible._</sup>

## More information

### Further reading on `IAsyncEnumerable`

- A good C#-based introduction [can be found in this blog][8].
- [An MSDN article][9] written shortly after it was introduced.
- Converting a `seq` to an `IAsyncEnumerable` [demo gist][10] as an example, though `TaskSeq` contains many more utility functions and uses a slightly different approach.

### Further reading on resumable state machines

- A state machine from a monadic perspective in F# [can be found here][12], which works with the pre-F# 6.0 non-resumable internals.
- The [original RFC for F# 6.0 on resumable state machines][13]
- The [original RFC for introducing `task`][14] to F# 6.0.
- A [pre F# 6.0 `TaskBuilder`][15] that motivated the `task` CE later added to F# Core.
- [MSDN Documentation on `task`][16] and [`async`][17].

### Further reading on computation expressions

- [Docs on MSDN][18] form a good summary and starting point.
- Arguably the best [step-by-step tutorial to using and building computation expressions][19] by Scott Wlaschin.

## Building & testing

TLDR: just run `build`. Or load the `sln` file in Visual Studio or VS Code and compile.

### Prerequisites

At the very least, to get the source to compile, you'll need:

- .NET 6 or .NET 7 Preview
- F# 6.0 or 7.0 compiler
- To use `build.cmd`, the `dotnet` command must be accessible from your path.

Just check-out this repo locally. Then, from the root of the repo, you can do:

### Build the solution

```bash
build [build] [release|debug]
```

With no arguments, defaults to `release`.

### Run the tests

```bash
build test [release|debug]
```

With no arguments, defaults to `release`. By default, all tests are output to the console. If you don't want that, you can use `--logger console;verbosity=summary`.
Furthermore, no TRX file is generated and the `--blame-xxx` flags aren't set.

### Run the CI command

```bash
build ci [release|debug]
```

With no arguments, defaults to `release`. This will run `dotnet test` with the `--blame-xxx` settings enabled to [prevent hanging tests][1] caused by
an [xUnit runner bug][2].

There are no special CI environment variables that need to be set for running this locally.

### Advanced

You can pass any additional options that are valid for `dotnet test` and `dotnet build` respectively. However,
these cannot be the very first argument, so you should either use `build build --myadditionalOptions fizz buzz`, or
just specify the build-kind, i.e. this is fine:

```bash
build debug --verbosity detailed
build test --logger console;verbosity=summary
```

At this moment, additional options cannot have quotes in them.

Command modifiers, like `release` and `debug`, can be specified with `-` or `/` if you so prefer: `dotnet build /release`.

### Get help

```bash
build help
```

For more info, see this PR: <https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/29>.

## Work in progress

The `taskSeq` CE using the statically compilable _resumable state machine_ approach is based on, and draw heavily from [Don Symes `taskSeq.fs`][20] as used to test the resumable state machine in the F# core compiler.

On top of that, this library adds a set of `TaskSeq` module functions, with their `Async` variants, on par with `Seq` and `AsyncSeq`.

## Current set of `TaskSeq` utility functions

The following are the current surface area of the `TaskSeq` utility functions, ordered alphabetically.

```f#
module TaskSeq =
    val append: source1: TaskSeq<'T> -> source2: TaskSeq<'T> -> TaskSeq<'T>
    val appendSeq: source1: TaskSeq<'T> -> source2: seq<'T> -> TaskSeq<'T>
    val box: source: TaskSeq<'T> -> TaskSeq<obj>
    val cast: source: TaskSeq<obj> -> TaskSeq<'T>
    val choose: chooser: ('T -> 'U option) -> source: TaskSeq<'T> -> TaskSeq<'U>
    val chooseAsync: chooser: ('T -> #Task<'U option>) -> source: TaskSeq<'T> -> TaskSeq<'U>
    val collect: binder: ('T -> #TaskSeq<'U>) -> source: TaskSeq<'T> -> TaskSeq<'U>
    val collectAsync: binder: ('T -> #Task<'TSeqU>) -> source: TaskSeq<'T> -> TaskSeq<'U> when 'TSeqU :> TaskSeq<'U>
    val collectSeq: binder: ('T -> #seq<'U>) -> source: TaskSeq<'T> -> TaskSeq<'U>
    val collectSeqAsync: binder: ('T -> #Task<'SeqU>) -> source: TaskSeq<'T> -> TaskSeq<'U> when 'SeqU :> seq<'U>
    val concat: sources: TaskSeq<#TaskSeq<'T>> -> TaskSeq<'T>
    val concat: sources: TaskSeq<'T seq> -> TaskSeq<'T>
    val concat: sources: TaskSeq<'T list> -> TaskSeq<'T>
    val concat: sources: TaskSeq<'T array> -> TaskSeq<'T>
    val concat: sources: TaskSeq<ResizeArray<'T>> -> TaskSeq<'T>
    val contains<'T when 'T: equality> : value: 'T -> source: TaskSeq<'T> -> Task<bool>
    val delay: generator: (unit -> TaskSeq<'T>) -> TaskSeq<'T>
    val drop: count: int -> source: TaskSeq<'T> -> TaskSeq<'T>
    val empty<'T> : TaskSeq<'T>
    val exactlyOne: source: TaskSeq<'T> -> Task<'T>
    val except<'T when 'T: equality> : itemsToExclude: TaskSeq<'T> -> source: TaskSeq<'T> -> TaskSeq<'T>
    val exceptOfSeq<'T when 'T: equality> : itemsToExclude: seq<'T> -> source: TaskSeq<'T> -> TaskSeq<'T>
    val exists: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<bool>
    val existsAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<bool>
    val filter: predicate: ('T -> bool) -> source: TaskSeq<'T> -> TaskSeq<'T>
    val filterAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> TaskSeq<'T>
    val find: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<'T>
    val findAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<'T>
    val findIndex: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<int>
    val findIndexAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<int>
    val fold: folder: ('State -> 'T -> 'State) -> state: 'State -> source: TaskSeq<'T> -> Task<'State>
    val foldAsync: folder: ('State -> 'T -> #Task<'State>) -> state: 'State -> source: TaskSeq<'T> -> Task<'State>
    val forall: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<bool>
    val forallAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<bool>
    val head: source: TaskSeq<'T> -> Task<'T>
    val indexed: source: TaskSeq<'T> -> TaskSeq<int * 'T>
    val init: count: int -> initializer: (int -> 'T) -> TaskSeq<'T>
    val initAsync: count: int -> initializer: (int -> #Task<'T>) -> TaskSeq<'T>
    val initInfinite: initializer: (int -> 'T) -> TaskSeq<'T>
    val initInfiniteAsync: initializer: (int -> #Task<'T>) -> TaskSeq<'T>
    val insertAt: position:int -> value:'T -> source: TaskSeq<'T> -> TaskSeq<'T>
    val insertManyAt: position:int -> values:TaskSeq<'T> -> source: TaskSeq<'T> -> TaskSeq<'T>
    val isEmpty: source: TaskSeq<'T> -> Task<bool>
    val item: index: int -> source: TaskSeq<'T> -> Task<'T>
    val iter: action: ('T -> unit) -> source: TaskSeq<'T> -> Task<unit>
    val iterAsync: action: ('T -> #Task<unit>) -> source: TaskSeq<'T> -> Task<unit>
    val iteri: action: (int -> 'T -> unit) -> source: TaskSeq<'T> -> Task<unit>
    val iteriAsync: action: (int -> 'T -> #Task<unit>) -> source: TaskSeq<'T> -> Task<unit>
    val last: source: TaskSeq<'T> -> Task<'T>
    val length: source: TaskSeq<'T> -> Task<int>
    val lengthBy: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<int>
    val lengthByAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<int>
    val lengthOrMax: max: int -> source: TaskSeq<'T> -> Task<int>
    val map: mapper: ('T -> 'U) -> source: TaskSeq<'T> -> TaskSeq<'U>
    val mapAsync: mapper: ('T -> #Task<'U>) -> source: TaskSeq<'T> -> TaskSeq<'U>
    val mapi: mapper: (int -> 'T -> 'U) -> source: TaskSeq<'T> -> TaskSeq<'U>
    val mapiAsync: mapper: (int -> 'T -> #Task<'U>) -> source: TaskSeq<'T> -> TaskSeq<'U>
    val max: source: TaskSeq<'T> -> Task<'T> when 'T: comparison
    val max: source: TaskSeq<'T> -> Task<'T> when 'T: comparison
    val maxBy: projection: ('T -> 'U) -> source: TaskSeq<'T> -> Task<'T> when 'U: comparison
    val minBy: projection: ('T -> 'U) -> source: TaskSeq<'T> -> Task<'T> when 'U: comparison
    val maxByAsync: projection: ('T -> #Task<'U>) -> source: TaskSeq<'T> -> Task<'T> when 'U: comparison
    val minByAsync: projection: ('T -> #Task<'U>) -> source: TaskSeq<'T> -> Task<'T> when 'U: comparison    val ofArray: source: 'T[] -> TaskSeq<'T>
    val ofAsyncArray: source: Async<'T> array -> TaskSeq<'T>
    val ofAsyncList: source: Async<'T> list -> TaskSeq<'T>
    val ofAsyncSeq: source: seq<Async<'T>> -> TaskSeq<'T>
    val ofList: source: 'T list -> TaskSeq<'T>
    val ofResizeArray: source: ResizeArray<'T> -> TaskSeq<'T>
    val ofSeq: source: seq<'T> -> TaskSeq<'T>
    val ofTaskArray: source: #Task<'T> array -> TaskSeq<'T>
    val ofTaskList: source: #Task<'T> list -> TaskSeq<'T>
    val ofTaskSeq: source: seq<#Task<'T>> -> TaskSeq<'T>
    val pick: chooser: ('T -> 'U option) -> source: TaskSeq<'T> -> Task<'U>
    val pickAsync: chooser: ('T -> #Task<'U option>) -> source: TaskSeq<'T> -> Task<'U>
    val prependSeq: source1: seq<'T> -> source2: TaskSeq<'T> -> TaskSeq<'T>
    val removeAt: position:int -> source: TaskSeq<'T> -> TaskSeq<'T>
    val removeManyAt: position:int -> count:int -> source: TaskSeq<'T> -> TaskSeq<'T>
    val singleton: source: 'T -> TaskSeq<'T>
    val skip: count: int -> source: TaskSeq<'T> -> TaskSeq<'T>
    val tail: source: TaskSeq<'T> -> Task<TaskSeq<'T>>
    val take: count: int -> source: TaskSeq<'T> -> TaskSeq<'T>
    val takeWhile: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<TaskSeq<'T>>
    val takeWhileAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<TaskSeq<'T>>
    val takeWhileInclusive: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<TaskSeq<'T>>
    val takeWhileInclusiveAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<TaskSeq<'T>>
    val toArray: source: TaskSeq<'T> -> 'T[]
    val toArrayAsync: source: TaskSeq<'T> -> Task<'T[]>
    val toIListAsync: source: TaskSeq<'T> -> Task<IList<'T>>
    val toList: source: TaskSeq<'T> -> 'T list
    val toListAsync: source: TaskSeq<'T> -> Task<'T list>
    val toResizeArrayAsync: source: TaskSeq<'T> -> Task<ResizeArray<'T>>
    val toSeq: source: TaskSeq<'T> -> seq<'T>
    val truncate: count: int -> source: TaskSeq<'T> -> TaskSeq<'T>
    val tryExactlyOne: source: TaskSeq<'T> -> Task<'T option>
    val tryFind: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<'T option>
    val tryFindAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<'T option>
    val tryFindIndex: predicate: ('T -> bool) -> source: TaskSeq<'T> -> Task<int option>
    val tryFindIndexAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> Task<int option>
    val tryHead: source: TaskSeq<'T> -> Task<'T option>
    val tryItem: index: int -> source: TaskSeq<'T> -> Task<'T option>
    val tryLast: source: TaskSeq<'T> -> Task<'T option>
    val tryPick: chooser: ('T -> 'U option) -> source: TaskSeq<'T> -> Task<'U option>
    val tryPickAsync: chooser: ('T -> #Task<'U option>) -> source: TaskSeq<'T> -> Task<'U option>
    val tryTail: source: TaskSeq<'T> -> Task<TaskSeq<'T> option>
    val where: predicate: ('T -> bool) -> source: TaskSeq<'T> -> TaskSeq<'T>
    val whereAsync: predicate: ('T -> #Task<bool>) -> source: TaskSeq<'T> -> TaskSeq<'T>
    val unbox<'U when 'U: struct> : source: TaskSeq<obj> -> TaskSeq<'U>
    val updateAt: position:int -> value:'T -> source: TaskSeq<'T> -> TaskSeq<'T>
    val zip: source1: TaskSeq<'T> -> source2: TaskSeq<'U> -> TaskSeq<'T * 'U>
```

[buildstatus]: https://github.com/fsprojects/FSharp.Control.TaskSeq/actions/workflows/main.yaml
[buildstatus_img]: https://github.com/fsprojects/FSharp.Control.TaskSeq/actions/workflows/main.yaml/badge.svg
[teststatus]: https://github.com/fsprojects/FSharp.Control.TaskSeq/actions/workflows/test.yaml
[teststatus_img]: https://github.com/fsprojects/FSharp.Control.TaskSeq/actions/workflows/test.yaml/badge.svg

[1]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/25
[2]: https://github.com/xunit/xunit/issues/2587
[3]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=net-6.0
[4]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerator-1.movenextasync?view=net-6.0
[5]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerator-1.current?view=net-6.0
[6]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1.getasyncenumerator?view=net-6.0
[7]: https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable?view=net-6.0
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
[20]: https://github.com/dotnet/fsharp/blob/d5312aae8aad650f0043f055bb14c3aa8117e12e/tests/benchmarks/CompiledCodeBenchmarks/TaskPerf/TaskPerf/taskSeq.fs
[21]: https://www.nuget.org/packages/FSharp.Control.TaskSeq#versions-body-tab
[22]: https://fsprojects.github.io/FSharp.Control.AsyncSeq/reference/fsharp-control-asyncseq.html#toAsyncEnum
[23]: https://fsprojects.github.io/FSharp.Control.AsyncSeq/reference/fsharp-control-asyncseq.html#fromAsyncEnum
[24]: https://github.com/TheAngryByrd/IcedTasks

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
[#90]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/90
[#126]: https://github.com/fsprojects/FSharp.Control.TaskSeq/pull/126
[#133]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/133
[#167]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/167
[#209]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/209
[#217]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/217
[#219]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/219
[#221]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/221
[#237]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/237
[#236]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/236
[#240]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/240

[issues]: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues
[nuget]: https://www.nuget.org/packages/FSharp.Control.TaskSeq/
