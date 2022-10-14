namespace FSharpy

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

module TaskSeq =
    // F# BUG: the following module is 'AutoOpen' and this isn't needed in the Tests project. Why do we need to open it?
    open FSharpy.TaskSeqBuilders

    // Just for convenience
    module Internal = TaskSeqInternal

    let empty<'T> = taskSeq {
        for c: 'T in [] do
            yield c
    }

    let isEmpty taskSeq = Internal.isEmpty taskSeq

    //
    // Convert 'ToXXX' functions
    //

    let toList (t: taskSeq<'T>) = [
        let e = t.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    ]


    let toArray (taskSeq: taskSeq<'T>) = [|
        let e = taskSeq.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    |]

    let toSeqCached (taskSeq: taskSeq<'T>) = seq {
        let e = taskSeq.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    }

    let toArrayAsync taskSeq =
        Internal.toResizeArrayAsync taskSeq
        |> Task.map (fun a -> a.ToArray())

    let toListAsync taskSeq = Internal.toResizeArrayAndMapAsync List.ofSeq taskSeq

    let toResizeArrayAsync taskSeq = Internal.toResizeArrayAsync taskSeq

    let toIListAsync taskSeq = Internal.toResizeArrayAndMapAsync (fun x -> x :> IList<_>) taskSeq

    let toSeqCachedAsync taskSeq = Internal.toResizeArrayAndMapAsync (fun x -> x :> seq<_>) taskSeq

    //
    // Convert 'OfXXX' functions
    //

    let ofArray (array: 'T[]) = taskSeq {
        for c in array do
            yield c
    }

    let ofList (list: 'T list) = taskSeq {
        for c in list do
            yield c
    }

    let ofSeq (sequence: 'T seq) = taskSeq {
        for c in sequence do
            yield c
    }

    let ofResizeArray (data: 'T ResizeArray) = taskSeq {
        for c in data do
            yield c
    }

    let ofTaskSeq (sequence: #Task<'T> seq) = taskSeq {
        for c in sequence do
            let! c = c
            yield c
    }

    let ofTaskList (list: #Task<'T> list) = taskSeq {
        for c in list do
            let! c = c
            yield c
    }

    let ofTaskArray (array: #Task<'T> array) = taskSeq {
        for c in array do
            let! c = c
            yield c
    }

    let ofAsyncSeq (sequence: Async<'T> seq) = taskSeq {
        for c in sequence do
            let! c = task { return! c }
            yield c
    }

    let ofAsyncList (list: Async<'T> list) = taskSeq {
        for c in list do
            let! c = Task.ofAsync c
            yield c
    }

    let ofAsyncArray (array: Async<'T> array) = taskSeq {
        for c in array do
            let! c = Async.toTask c
            yield c
    }


    //
    // iter/map/collect functions
    //

    let iter action taskSeq = Internal.iter (SimpleAction action) taskSeq

    let iteri action taskSeq = Internal.iter (CountableAction action) taskSeq

    let iterAsync action taskSeq = Internal.iter (AsyncSimpleAction action) taskSeq

    let iteriAsync action taskSeq = Internal.iter (AsyncCountableAction action) taskSeq

    let map (mapper: 'T -> 'U) taskSeq = Internal.map (SimpleAction mapper) taskSeq

    let mapi (mapper: int -> 'T -> 'U) taskSeq = Internal.map (CountableAction mapper) taskSeq

    let mapAsync mapper taskSeq = Internal.map (AsyncSimpleAction mapper) taskSeq

    let mapiAsync mapper taskSeq = Internal.map (AsyncCountableAction mapper) taskSeq

    let collect (binder: 'T -> #IAsyncEnumerable<'U>) taskSeq = Internal.collect binder taskSeq

    let collectSeq (binder: 'T -> #seq<'U>) taskSeq = Internal.collectSeq binder taskSeq

    let collectAsync (binder: 'T -> #Task<#IAsyncEnumerable<'U>>) taskSeq : taskSeq<'U> =
        Internal.collectAsync binder taskSeq

    let collectSeqAsync (binder: 'T -> #Task<#seq<'U>>) taskSeq : taskSeq<'U> = Internal.collectSeqAsync binder taskSeq

    //
    // choosers, pickers and the like
    //

    let tryHead taskSeq = Internal.tryHead taskSeq

    let head taskSeq = task {
        match! Internal.tryHead taskSeq with
        | Some head -> return head
        | None -> return Internal.raiseEmptySeq ()
    }

    let tryLast taskSeq = Internal.tryLast taskSeq

    let last taskSeq = task {
        match! Internal.tryLast taskSeq with
        | Some last -> return last
        | None -> return Internal.raiseEmptySeq ()
    }

    let tryItem index taskSeq = Internal.tryItem index taskSeq

    let item index taskSeq = task {
        match! Internal.tryItem index taskSeq with
        | Some item -> return item
        | None ->
            if index < 0 then
                return invalidArg (nameof index) "The input must be non-negative."
            else
                return Internal.raiseInsufficient ()
    }

    let tryExactlyOne source = Internal.tryExactlyOne source

    let exactlyOne source = task {
        match! Internal.tryExactlyOne source with
        | Some item -> return item
        | None -> return invalidArg (nameof source) "The input sequence contains more than one element."
    }

    let choose chooser source = Internal.choose (TryPick chooser) source
    let chooseAsync chooser source = Internal.choose (TryPickAsync chooser) source
    let filter predicate source = Internal.filter (TryFilter predicate) source
    let filterAsync predicate source = Internal.filter (TryFilterAsync predicate) source
    let tryPick chooser source = Internal.tryPick (TryPick chooser) source
    let tryPickAsync chooser source = Internal.tryPick (TryPickAsync chooser) source
    let tryFind predicate source = Internal.tryFind (TryFilter predicate) source
    let tryFindAsync predicate source = Internal.tryFind (TryFilterAsync predicate) source

    let pick chooser source = task {
        match! Internal.tryPick (TryPick chooser) source with
        | Some item -> return item
        | None -> return Internal.raiseNotFound ()
    }

    let pickAsync chooser source = task {
        match! Internal.tryPick (TryPickAsync chooser) source with
        | Some item -> return item
        | None -> return Internal.raiseNotFound ()
    }

    let find predicate source = task {
        match! Internal.tryFind (TryFilter predicate) source with
        | Some item -> return item
        | None -> return Internal.raiseNotFound ()
    }

    let findAsync predicate source = task {
        match! Internal.tryFind (TryFilterAsync predicate) source with
        | Some item -> return item
        | None -> return Internal.raiseNotFound ()
    }

    //
    // zip/unzip etc functions
    //

    let zip taskSeq1 taskSeq2 = Internal.zip taskSeq1 taskSeq2

    let fold folder state taskSeq = Internal.fold (FolderAction folder) state taskSeq

    let foldAsync folder state taskSeq = Internal.fold (AsyncFolderAction folder) state taskSeq
