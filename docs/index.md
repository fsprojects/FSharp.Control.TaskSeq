# FSharp.Control.TaskSeq

FSharp.Control.TaskSeq provides a `taskSeq` computation expression for `IAsyncEnumerable<'T>`,
along with [a comprehensive `TaskSeq` module](https://fsprojects.github.io/FSharp.Control.TaskSeq/reference/fsharp-control-taskseq.html).

An **task sequence** is an asynchronous sequence in which individual elements are _awaited_:
the next element is not necessarily available immediately.
Under the hood each `taskSeq { ... }` is an `IAsyncEnumerable<'T>` — the .NET standard
interface used by C# `await foreach`, Entity Framework Core, gRPC streaming, and many other
libraries in the ecosystem.

## Quick Start

Add the [NuGet package `FSharp.Control.TaskSeq`](https://www.nuget.org/packages/FSharp.Control.TaskSeq)
to your project and open the namespace:

```fsharp
// #r "nuget: FSharp.Control.TaskSeq"

open FSharp.Control
```

A `TaskSeq<'T>` can be created with the `taskSeq { ... }` computation expression:

```fsharp
let oneThenTwo = taskSeq {
    yield 1
    do! Task.Delay 1000   // non-blocking sleep
    yield 2
}
```

When iterated, the sequence above yields `1` immediately and `2` after one second. Consumers
must _await_ each step.

Use `await foreach` in C#, or any `TaskSeq` consumer in F#:

```fsharp
// Iterate with a for loop inside a task { ... }
task {
    for item in oneThenTwo do
        printfn "Got %d" item
} |> Task.Run |> ignore

// Or use TaskSeq.iter
do! oneThenTwo |> TaskSeq.iter (printfn "Got %d")
```

## Topics

* [Generating sequences](TaskSeqGenerating.fsx) — `taskSeq { }`, `init`, `unfold`, `ofArray`, …
* [Transforming sequences](TaskSeqTransforming.fsx) — `map`, `filter`, `choose`, `collect`, …
* [Consuming sequences](TaskSeqConsuming.fsx) — `iter`, `fold`, `find`, `toArray`, `sum`, …
* [Combining sequences](TaskSeqCombining.fsx) — `append`, `zip`, `take`, `skip`, `chunkBySize`, …
* [Advanced operations](TaskSeqAdvanced.fsx) — `groupBy`, `mapFold`, `distinct`, `partition`, …

## Comparison with FSharp.Control.AsyncSeq

[FSharp.Control.AsyncSeq](https://github.com/fsprojects/FSharp.Control.AsyncSeq/) is the
predecessor library, oriented towards `Async<'T>`.
Both libraries implement `IAsyncEnumerable<'T>` so they are interoperable.
The main differences are:

| | `TaskSeq` | `AsyncSeq` |
|---|---|---|
| Async model | `Task` / `ValueTask` | `Async` |
| Performance | Higher (resumable state machines) | Good |
| .NET interop | Native `IAsyncEnumerable<'T>` | Native `IAsyncEnumerable<'T>` |
| Cancellation | `CancellationToken` via `withCancellation` | Built in to `Async` |

## Related links

* [NuGet package](https://www.nuget.org/packages/FSharp.Control.TaskSeq)
* [GitHub repository](https://github.com/fsprojects/FSharp.Control.TaskSeq)
* [Release notes](https://github.com/fsprojects/FSharp.Control.TaskSeq/blob/main/release-notes.txt)
* [API reference](reference/index.html)
