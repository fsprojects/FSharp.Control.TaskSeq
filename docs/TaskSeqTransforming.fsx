(**
---
title: Transforming Task Sequences
category: Documentation
categoryindex: 2
index: 3
description: How to transform and filter F# task sequences using map, filter, choose, collect, and related combinators.
keywords: F#, task sequences, TaskSeq, IAsyncEnumerable, map, mapAsync, filter, filterAsync, choose, collect, indexed, cast
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

# Transforming Task Sequences

This page covers the core operations for transforming `TaskSeq<'T>` values: `map`, `filter`,
`choose`, `collect`, `indexed`, and type conversions.
For operations that consume a sequence into a single result, see
[Consuming Sequences](TaskSeqConsuming.fsx).

*)

open System.Threading.Tasks
open FSharp.Control

(**

## Transforming with a computation expression

The most flexible way to transform a task sequence is to write a function that accepts a
`TaskSeq<_>` and returns a `TaskSeq<_>` using a `taskSeq { ... }` computation expression. The
`for` loop inside the CE is an asynchronous loop that awaits each element:

*)

let labelEvenOdd (input: TaskSeq<int>) : TaskSeq<string> =
    taskSeq {
        for n in input do
            if n % 2 = 0 then
                do! Task.Delay 10 // simulate async work
                yield $"Even: {n}"
            else
                yield $"Odd: {n}"
    }

(**

Inside `taskSeq { ... }` you can freely mix synchronous logic, `let!`/`do!` awaits, loops,
and conditionals.

---

## map and mapAsync

`TaskSeq.map` applies a synchronous function to every element:

*)

let numbers : TaskSeq<int> = TaskSeq.ofSeq (seq { 1..5 })

let doubled : TaskSeq<int> = numbers |> TaskSeq.map (fun n -> n * 2)

(**

`TaskSeq.mapAsync` is the same but the projection returns `Task<'U>`, allowing async work per
element — for example, a database lookup or HTTP request:

*)

let fetchDescription (n: int) : Task<string> =
    task { return $"item {n}" } // placeholder for a real async call

let descriptions : TaskSeq<string> = numbers |> TaskSeq.mapAsync fetchDescription

(**

### mapi and mapiAsync

`TaskSeq.mapi` and `TaskSeq.mapiAsync` additionally pass the zero-based index to the mapper:

*)

let indexed : TaskSeq<string> =
    numbers |> TaskSeq.mapi (fun i n -> $"[{i}] {n}")

(**

---

## filter and filterAsync

`TaskSeq.filter` keeps only elements satisfying a synchronous predicate:

*)

let evens : TaskSeq<int> = numbers |> TaskSeq.filter (fun n -> n % 2 = 0)

(**

`TaskSeq.filterAsync` does the same with an async predicate — useful when the keep/discard
decision requires an async lookup:

*)

let isInteresting (n: int) : Task<bool> =
    task { return n > 2 } // placeholder

let interesting : TaskSeq<int> = numbers |> TaskSeq.filterAsync isInteresting

(**

`TaskSeq.where` and `TaskSeq.whereAsync` are aliases for `filter` and `filterAsync` provided
for readability.

---

## choose and chooseAsync

`TaskSeq.choose` applies a function that returns `'U option`; only `Some` values are kept and
the option wrapper is removed — it is equivalent to `filter` + `map` in a single pass:

*)

let strings : TaskSeq<string> =
    TaskSeq.ofList [ ""; "hello"; ""; "world" ]

let nonEmpty : TaskSeq<string> =
    strings
    |> TaskSeq.choose (fun s ->
        if s.Length > 0 then Some s else None)

(**

`TaskSeq.chooseAsync` accepts an async chooser:

*)

let parseAsync (s: string) : Task<int option> =
    task {
        match System.Int32.TryParse s with
        | true, n -> return Some n
        | _ -> return None
    }

let parsed : TaskSeq<int> =
    TaskSeq.ofList [ "1"; "two"; "3" ]
    |> TaskSeq.chooseAsync parseAsync

(**

---

## collect and collectSeq

`TaskSeq.collect` is the monadic bind for `TaskSeq`: it maps each element to a new task
sequence and concatenates all the results end-to-end:

*)

let words : TaskSeq<string> =
    TaskSeq.ofList [ "foo bar"; "baz qux" ]

let chars : TaskSeq<string> =
    words
    |> TaskSeq.collectSeq (fun sentence ->
        sentence.Split(' ') |> Array.toSeq)

(**

`TaskSeq.collect` maps to another `TaskSeq<'U>`, while `TaskSeq.collectSeq` maps to a plain
`seq<'U>`. Async variants `TaskSeq.collectAsync` and `TaskSeq.collectSeqAsync` accept mappers
that return tasks.

---

## indexed

`TaskSeq.indexed` pairs each element with its zero-based index, returning a
`TaskSeq<int * 'T>`:

*)

let withIndex : TaskSeq<int * string> =
    TaskSeq.ofList [ "a"; "b"; "c" ] |> TaskSeq.indexed

// yields (0,"a"), (1,"b"), (2,"c")

(**

---

## Type conversions

`TaskSeq.cast` casts items from `TaskSeq<obj>` to a target reference type. For value types use
`TaskSeq.unbox`:

*)

let boxed : TaskSeq<obj> =
    TaskSeq.ofList [ box 1; box 2; box 3 ]

let unboxed : TaskSeq<int> = boxed |> TaskSeq.unbox<int>

let castToString : TaskSeq<string> =
    TaskSeq.ofList [ box "hello"; box "world" ]
    |> TaskSeq.cast<string>
