# FSharp.Control.TaskSeq

An implementation of `IAsyncEnumerable<'T>` as a computation expression: `taskSeq { ... }` with an accompanying `TaskSeq` module and functions, that allow seamless use of asynchronous sequences similar to F#'s native `seq` and `task` CE's.

## Documentation

See https://fsprojects.github.io/FSharp.Control.TaskSeq/

## Known issues

- **Debug-mode truncation with recent .NET SDKs (SDK `10.0.400`+ / F# 10+)**: a `taskSeq { }` sequence that awaits (`do!`, `let!`, `Task.Delay`, etc.) between `yield`s can silently stop early when the *consuming* project is built in **Debug** configuration — `MoveNextAsync` returns `false` after the first await, with no exception or warning. **Release** builds (`-c Release` / `-p:Optimize=true`) are unaffected. This is not a bug in FSharp.Control.TaskSeq: it is an upstream F# compiler regression in Debug-mode state machine lowering for composed `ResumableCode` builders like this library's, tracked as [dotnet/fsharp#20466](https://github.com/dotnet/fsharp/issues/20466) with a fix in progress at [dotnet/fsharp#20469](https://github.com/dotnet/fsharp/pull/20469). If you observe unexpectedly short `taskSeq` sequences (for example in `dotnet test`, which defaults to Debug), try building/running in Release as a workaround; see #473 for details.
