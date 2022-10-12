namespace FSharpy

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open FSharpy.TaskSeqBuilders

[<AutoOpen>]
module ExtraTaskSeqOperators =
    /// A TaskSeq workflow for IAsyncEnumerable<'T> types.
    let taskSeq = TaskSeqBuilder()

module internal TaskSeqInternal =
    let iteriAsync action (taskSeq: taskSeq<_>) = task {
        let e = taskSeq.GetAsyncEnumerator(CancellationToken())
        let mutable go = true
        let mutable i = 0
        let! step = e.MoveNextAsync()
        go <- step

        while go do
            action i e.Current
            let! step = e.MoveNextAsync()
            i <- i + 1
            go <- step
    }

    let fold (action: 'State -> 'T -> 'State) initial (taskSeq: taskSeq<_>) = task {
        let e = taskSeq.GetAsyncEnumerator(CancellationToken())
        let mutable go = true
        let mutable result = initial
        let! step = e.MoveNextAsync()
        go <- step

        while go do
            result <- action result e.Current
            let! step = e.MoveNextAsync()
            go <- step

        return result
    }

    let toResizeArrayAsync taskSeq = task {
        let res = ResizeArray()
        do! taskSeq |> iteriAsync (fun _ item -> res.Add item)
        return res
    }

    let mapi mapper (taskSequence: taskSeq<_>) = taskSeq {
        let mutable i = 0

        for c in taskSequence do
            yield mapper i c
            i <- i + 1
    }

    let mapiAsync (mapper: _ -> _ -> Task<'T>) (taskSequence: taskSeq<_>) = taskSeq {
        let mutable i = 0

        for c in taskSequence do
            let! x = mapper i c
            yield x
            i <- i + 1
    }

    let zip (taskSequence1: taskSeq<_>) (taskSequence2: taskSeq<_>) = taskSeq {
        let e1 = taskSequence1.GetAsyncEnumerator(CancellationToken())
        let e2 = taskSequence2.GetAsyncEnumerator(CancellationToken())
        let mutable go = true
        let! step1 = e1.MoveNextAsync()
        let! step2 = e1.MoveNextAsync()
        go <- step1 && step2

        while go do
            yield e1.Current, e2.Current
            let! step1 = e1.MoveNextAsync()
            let! step2 = e1.MoveNextAsync()
            go <- step1 && step2

        if step1 then
            invalidArg "taskSequence1" "The task sequences had different lengths."

        if step2 then
            invalidArg "taskSequence2" "The task sequences had different lengths."
    }

    let collect (binder: _ -> #IAsyncEnumerable<_>) (taskSequence: taskSeq<_>) = taskSeq {
        for c in taskSequence do
            yield! binder c :> IAsyncEnumerable<_>
    }

    let collectSeq (binder: _ -> #seq<_>) (taskSequence: taskSeq<_>) = taskSeq {
        for c in taskSequence do
            yield! binder c :> seq<_>
    }

    /// Returns taskSeq as an array. This function is blocking until the sequence is exhausted.
    let toListResult (t: taskSeq<'T>) = [
        let e = t.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    ]
