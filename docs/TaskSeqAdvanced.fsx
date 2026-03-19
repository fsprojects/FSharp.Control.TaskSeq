(**
---
title: Advanced Task Sequence Operations
category: Documentation
categoryindex: 2
index: 6
description: Advanced F# task sequence operations including groupBy, mapFold, threadState, distinct, partition, countBy, compareWith, withCancellation and in-place editing.
keywords: F#, task sequences, TaskSeq, IAsyncEnumerable, groupBy, mapFold, threadState, distinct, distinctUntilChanged, except, partition, countBy, compareWith, withCancellation, insertAt, removeAt, updateAt
---
*)
(*** condition: prepare ***)
#nowarn "211"
#I "../src/FSharp.Control.TaskSeq/bin/Release/netstandard2.1"
#r "FSharp.Control.TaskSeq.dll"
(*** condition: fsx ***)
#if FSX
#r "nuget: FSharp.Control.TaskSeq,{{fsdocs-package-version}}"
#endif // FSX
(*** condition: ipynb ***)
#if IPYNB
#r "nuget: FSharp.Control.TaskSeq,{{fsdocs-package-version}}"
#endif // IPYNB

(**

# Advanced Task Sequence Operations

This page covers advanced `TaskSeq<'T>` operations: grouping, stateful transformation with
`mapFold` and `threadState`, deduplication, set-difference, partitioning, counting by key, lexicographic
comparison, cancellation, and positional editing.

*)

open System.Threading
open System.Threading.Tasks
open FSharp.Control

(**

---

## groupBy and groupByAsync

`TaskSeq.groupBy` partitions a sequence into groups by a key-projection function.  The result
is an array of `(key, elements[])` pairs, one per distinct key, in order of first occurrence.

> **Note:** `groupBy` consumes the entire source before returning.  Do not use it on
> potentially infinite sequences.

*)

type Event = { EntityId: int; Payload: string }

let events : TaskSeq<Event> =
    TaskSeq.ofList
        [ { EntityId = 1; Payload = "A" }
          { EntityId = 2; Payload = "B" }
          { EntityId = 1; Payload = "C" }
          { EntityId = 3; Payload = "D" }
          { EntityId = 2; Payload = "E" } ]

// groups: (1, [A;C]), (2, [B;E]), (3, [D])
let grouped : Task<(int * Event[])[]> =
    events |> TaskSeq.groupBy (fun e -> e.EntityId)

(**

`TaskSeq.groupByAsync` accepts an async key projection:

*)

let groupedAsync : Task<(int * Event[])[]> =
    events |> TaskSeq.groupByAsync (fun e -> task { return e.EntityId })

(**

---

## countBy and countByAsync

`TaskSeq.countBy` counts how many elements map to each key, returning `(key, count)[]`:

*)

let counts : Task<(int * int)[]> =
    events |> TaskSeq.countBy (fun e -> e.EntityId)
// (1,2), (2,2), (3,1)

(**

---

## mapFold and mapFoldAsync

`TaskSeq.mapFold` threads a state accumulator through a sequence while simultaneously mapping
each element to a result value.  The output is a task returning a pair of `(result[], finalState)`:

*)

// Number each word sequentially while building a running concatenation
let words : TaskSeq<string> =
    TaskSeq.ofList [ "hello"; "world"; "foo" ]

let numbered : Task<string[] * int> =
    words
    |> TaskSeq.mapFold (fun count w -> $"{count}: {w}", count + 1) 0

// result: ([| "0: hello"; "1: world"; "2: foo" |], 3)

(**

`TaskSeq.mapFoldAsync` is the same but the mapping function returns `Task<'Result * 'State>`.

---

## threadState and threadStateAsync

`TaskSeq.threadState` is the lazy, streaming counterpart to `mapFold`.  It threads a state
accumulator through the sequence while yielding each mapped result — but unlike `mapFold` it
never materialises the results into an array, and it discards the final state.  This makes it
suitable for infinite sequences and pipelines where intermediate results should be streamed rather
than buffered:

*)

let numbers : TaskSeq<int> = TaskSeq.ofSeq (seq { 1..5 })

// Produce a running total without collecting the whole sequence first
let runningSum : TaskSeq<int> =
    numbers
    |> TaskSeq.threadState (fun acc x -> acc + x, acc + x) 0

// yields lazily: 1, 3, 6, 10, 15

(**

Compare with `scan`, which also emits a running result but prepends the initial state:

```fsharp
let viaScan = numbers |> TaskSeq.scan (fun acc x -> acc + x) 0
// yields: 0, 1, 3, 6, 10, 15  (one extra initial element)

let viaThreadState = numbers |> TaskSeq.threadState (fun acc x -> acc + x, acc + x) 0
// yields: 1, 3, 6, 10, 15  (no initial element; result == new state here)
```

`TaskSeq.threadStateAsync` accepts an asynchronous folder:

*)

let asyncRunningSum : TaskSeq<int> =
    numbers
    |> TaskSeq.threadStateAsync (fun acc x -> Task.fromResult (acc + x, acc + x)) 0

(**

`TaskSeq.scan` is the streaming sibling of `fold`: it emits each intermediate state as a new
element, starting with the initial state:

*)

let runningTotals : TaskSeq<int> =
    numbers |> TaskSeq.scan (fun acc n -> acc + n) 0

// yields: 0, 1, 3, 6, 10, 15

(**

---

## distinct and distinctBy

`TaskSeq.distinct` removes duplicates (keeps first occurrence), using generic equality:

*)

let withDups : TaskSeq<int> = TaskSeq.ofList [ 1; 2; 2; 3; 1; 4 ]

let deduped : TaskSeq<int> = withDups |> TaskSeq.distinct // 1, 2, 3, 4

(**

`TaskSeq.distinctBy` deduplicates by a key projection:

*)

let strings : TaskSeq<string> =
    TaskSeq.ofList [ "hello"; "HELLO"; "world"; "WORLD" ]

let caseInsensitiveDistinct : TaskSeq<string> =
    strings |> TaskSeq.distinctBy (fun s -> s.ToLowerInvariant())
// "hello", "world"

(**

> **Note:** both `distinct` and `distinctBy` buffer all unique keys in a hash set.  Do not use
> them on potentially infinite sequences.

`TaskSeq.distinctByAsync` accepts an async key projection.

---

## distinctUntilChanged

`TaskSeq.distinctUntilChanged` removes consecutive duplicates only — it does not buffer the
whole sequence, so it is safe on infinite streams:

*)

let run : TaskSeq<int> = TaskSeq.ofList [ 1; 1; 2; 2; 2; 3; 1; 1 ]

let noConsecDups : TaskSeq<int> = run |> TaskSeq.distinctUntilChanged
// 1, 2, 3, 1

(**

---

## except and exceptOfSeq

`TaskSeq.except itemsToExclude source` returns elements of `source` that do not appear in
`itemsToExclude`. The exclusion set is materialised eagerly before iteration:

*)

let exclusions : TaskSeq<int> = TaskSeq.ofList [ 2; 4 ]
let source : TaskSeq<int> = TaskSeq.ofSeq (seq { 1..5 })

let filtered : TaskSeq<int> = TaskSeq.except exclusions source // 1, 3, 5

(**

`TaskSeq.exceptOfSeq` accepts a plain `seq<'T>` as the exclusion set.

---

## partition and partitionAsync

`TaskSeq.partition` splits the sequence into two arrays in a single pass.  Elements for which
the predicate returns `true` go into the first array; the rest into the second:

*)

let partitioned : Task<int[] * int[]> =
    source |> TaskSeq.partition (fun n -> n % 2 = 0)
// trueItems: [|2;4|]   falseItems: [|1;3;5|]

(**

`TaskSeq.partitionAsync` accepts an async predicate.

---

## compareWith and compareWithAsync

`TaskSeq.compareWith` performs a lexicographic comparison of two sequences using a custom
comparer.  It returns the first non-zero comparison result, or `0` if the sequences are
element-wise equal and have the same length:

*)

let a : TaskSeq<int> = TaskSeq.ofList [ 1; 2; 3 ]
let b : TaskSeq<int> = TaskSeq.ofList [ 1; 2; 4 ]

let cmp : Task<int> =
    TaskSeq.compareWith (fun x y -> compare x y) a b
// negative (a < b)

(**

`TaskSeq.compareWithAsync` accepts an async comparer.

---

## withCancellation

`TaskSeq.withCancellation token source` injects a `CancellationToken` into the underlying
`IAsyncEnumerable<'T>`.  This is equivalent to calling `.WithCancellation(token)` in C# and
is useful when consuming sequences from libraries (e.g. Entity Framework Core) that require a
token at the enumeration site:

*)

let cts = new CancellationTokenSource()

let cancellable : TaskSeq<int> =
    source |> TaskSeq.withCancellation cts.Token

(**

---

## Positional editing

`TaskSeq.insertAt`, `TaskSeq.insertManyAt`, `TaskSeq.removeAt`, `TaskSeq.removeManyAt`, and
`TaskSeq.updateAt` produce new sequences with an element inserted, removed, or replaced at a
given zero-based index:

*)

let original : TaskSeq<int> = TaskSeq.ofList [ 1; 2; 4; 5 ]

let inserted : TaskSeq<int> = original |> TaskSeq.insertAt 2 3 // 1,2,3,4,5

let removed : TaskSeq<int> = original |> TaskSeq.removeAt 1 // 1,4,5

let updated : TaskSeq<int> = original |> TaskSeq.updateAt 0 99 // 99,2,4,5

let manyInserted : TaskSeq<int> =
    original
    |> TaskSeq.insertManyAt 2 (TaskSeq.ofList [ 10; 11 ])
// 1, 2, 10, 11, 4, 5

let manyRemoved : TaskSeq<int> = original |> TaskSeq.removeManyAt 1 2 // 1, 5
