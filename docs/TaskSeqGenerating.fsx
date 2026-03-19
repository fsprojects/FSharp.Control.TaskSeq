(**
---
title: Generating Task Sequences
category: Documentation
categoryindex: 2
index: 2
description: How to create F# task sequences using the taskSeq computation expression, init, unfold, and conversion functions.
keywords: F#, task sequences, TaskSeq, IAsyncEnumerable, taskSeq, computation expression, init, unfold, ofArray, ofSeq, singleton, replicate, replicateInfinite, replicateUntilNoneAsync
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

# Generating Task Sequences

This page covers the main ways to create `TaskSeq<'T>` values: the `taskSeq` computation
expression, factory functions such as `TaskSeq.init` and `TaskSeq.unfold`, and conversion
functions that wrap existing collections.

*)

open System.Threading.Tasks
open FSharp.Control

(**

## Computation Expression Syntax

`taskSeq { ... }` is a computation expression that lets you write asynchronous sequences
using familiar F# constructs.  Under the hood it compiles to a resumable state machine, so
there is no allocation per element.

### Yielding values

Use `yield` to emit a single value and `yield!` to splice in another sequence:

*)

let helloWorld = taskSeq {
    yield "hello"
    yield "world"
}

let combined = taskSeq {
    yield! helloWorld
    yield "!"
}

(**

### Conditionals

`if`/`then`/`else` works just like in ordinary F#:

*)

let evenNumbers = taskSeq {
    for i in 1..10 do
        if i % 2 = 0 then
            yield i
}

(**

### Match expressions

`match` expressions are fully supported:

*)

type Shape =
    | Circle of radius: float
    | Rectangle of width: float * height: float

let areas = taskSeq {
    for shape in [ Circle 3.0; Rectangle(4.0, 5.0); Circle 1.5 ] do
        match shape with
        | Circle r -> yield System.Math.PI * r * r
        | Rectangle(w, h) -> yield w * h
}

(**

### For loops

`for` iterates over any `seq<'T>`/`IEnumerable<'T>` synchronously, or over another
`TaskSeq<'T>` asynchronously:

*)

let squaresOfList = taskSeq {
    for n in [ 1; 2; 3; 4; 5 ] do
        yield n * n
}

// Iterate another TaskSeq
let doubled = taskSeq {
    for n in squaresOfList do
        yield n * 2
}

(**

### While loops

`while` emits elements until a condition becomes false.  Async operations can appear in the
loop body:

*)

let countdown = taskSeq {
    let mutable i = 5

    while i > 0 do
        yield i
        do! Task.Delay 100
        i <- i - 1
}

(**

### Awaiting tasks with `let!` and `do!`

Inside `taskSeq { ... }` you can await any `Task<'T>` with `let!` and any `Task<unit>` with
`do!`:

*)

let fetchData (url: string) : Task<string> =
    task { return $"data from {url}" } // placeholder

let results = taskSeq {
    for url in [ "https://example.com/a"; "https://example.com/b" ] do
        let! data = fetchData url
        yield data
}

(**

### Use bindings and try / with

`use` and `use!` dispose the resource when the sequence finishes or is abandoned. `try/with`
and `try/finally` work as expected:

*)

let withResource = taskSeq {
    use resource = { new System.IDisposable with member _.Dispose() = () } // placeholder
    yield 1
    yield 2
}

let withErrorHandling = taskSeq {
    try
        yield 1
        failwith "oops"
        yield 2
    with ex ->
        yield -1
}

(**

---

## init and initInfinite

`TaskSeq.init count initializer` generates `count` elements by calling `initializer` with the
zero-based index:

*)

// [| 0; 1; 4; 9; 16 |]
let squares : TaskSeq<int> = TaskSeq.init 5 (fun i -> i * i)

// With an async initializer
let asyncSquares : TaskSeq<int> =
    TaskSeq.initAsync 5 (fun i -> task { return i * i })

(**

`TaskSeq.initInfinite` generates an unbounded sequence — use `TaskSeq.take` or
`TaskSeq.takeWhile` to limit consumption:

*)

let naturals : TaskSeq<int> = TaskSeq.initInfinite id

let first10 : TaskSeq<int> = naturals |> TaskSeq.take 10 // 0 .. 9

(**

---

## unfold

`TaskSeq.unfold` derives a sequence from a state value.  Each call to the generator returns
either `None` (end) or `Some (element, nextState)`:

*)

// 0, 1, 2, ... up to but not including 5
let counting : TaskSeq<int> =
    TaskSeq.unfold
        (fun state ->
            if state < 5 then
                Some(state, state + 1)
            else
                None)
        0

// Same with an async generator
let countingAsync : TaskSeq<int> =
    TaskSeq.unfoldAsync
        (fun state ->
            task {
                if state < 5 then
                    return Some(state, state + 1)
                else
                    return None
            })
        0

(**

---

## singleton, replicate, replicateInfinite, and empty

*)

let one : TaskSeq<string> = TaskSeq.singleton "hello"

let fives : TaskSeq<int> = TaskSeq.replicate 3 5 // 5, 5, 5

let nothing : TaskSeq<int> = TaskSeq.empty<int>

(**

`TaskSeq.replicateInfinite` yields a constant value indefinitely.  Always combine it with a
bounding operation such as `take` or `takeWhile`:

*)

let infinitePings : TaskSeq<string> = TaskSeq.replicateInfinite "ping"

let first10pings : TaskSeq<string> = infinitePings |> TaskSeq.take 10

(**

`TaskSeq.replicateInfiniteAsync` calls a function on every step, useful for polling or streaming
side-effectful sources:

*)

let mutable counter = 0

let pollingSeq : TaskSeq<int> =
    TaskSeq.replicateInfiniteAsync (fun () ->
        task {
            counter <- counter + 1
            return counter
        })

let first5counts : TaskSeq<int> = pollingSeq |> TaskSeq.take 5

(**

`TaskSeq.replicateUntilNoneAsync` stops when the function returns `None`, making it easy to
wrap a pull-based source that signals end-of-stream with `None`:

*)

let readLine (reader: System.IO.TextReader) =
    TaskSeq.replicateUntilNoneAsync (fun () ->
        task {
            let! line = reader.ReadLineAsync()
            return if line = null then None else Some line
        })

(**

---

## delay

`TaskSeq.delay` creates a sequence whose body is not evaluated until iteration starts.  This
is useful when the sequence depends on side-effectful initialisation:

*)

let deferred =
    TaskSeq.delay (fun () ->
        taskSeq {
            printfn "sequence started"
            yield! [ 1; 2; 3 ]
        })

(**

---

## Converting from existing collections

All of these produce a `TaskSeq<'T>` that replays the source on each iteration:

*)

let fromArray : TaskSeq<int> = TaskSeq.ofArray [| 1; 2; 3 |]
let fromList : TaskSeq<int> = TaskSeq.ofList [ 1; 2; 3 ]
let fromSeq : TaskSeq<int> = TaskSeq.ofSeq (seq { 1..3 })
let fromResizeArray : TaskSeq<int> = TaskSeq.ofResizeArray (System.Collections.Generic.List<int>([ 1; 2; 3 ]))

(**

You can also wrap existing task or async collections.  Note that wrapping a list of already
started `Task`s does **not** guarantee sequential execution—the tasks are already running:

*)

// Sequence of not-yet-started task factories (safe)
let fromTaskSeq : TaskSeq<int> =
    TaskSeq.ofTaskSeq (seq { yield task { return 1 }; yield task { return 2 } })

// Sequence of asyncs (each started on demand as the sequence is consumed)
let fromAsyncSeq : TaskSeq<int> =
    TaskSeq.ofAsyncSeq (seq { yield async { return 1 }; yield async { return 2 } })
