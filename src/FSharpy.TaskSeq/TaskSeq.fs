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

    let isEmpty source = Internal.isEmpty source

    //
    // Convert 'ToXXX' functions
    //

    let toList (source: taskSeq<'T>) = [
        let e = source.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    ]

    let format x = string x
    let f () = format 42


    let toArray (source: taskSeq<'T>) = [|
        let e = source.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    |]

    let toSeqCached (source: taskSeq<'T>) = seq {
        let e = source.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    }

    // FIXME: incomplete and incorrect code!!!
    let toSeqOfTasks (source: taskSeq<'T>) = seq {
        let e = source.GetAsyncEnumerator(CancellationToken())

        // TODO: check this!
        try
            let mutable go = false

            while go do
                yield task {
                    let! step = e.MoveNextAsync()
                    go <- step

                    if step then
                        return e.Current
                    else
                        return Unchecked.defaultof<_> // FIXME!
                }

        finally
            e.DisposeAsync().AsTask().Wait()
    }

    let toArrayAsync source =
        Internal.toResizeArrayAsync source
        |> Task.map (fun a -> a.ToArray())

    let toListAsync source = Internal.toResizeArrayAndMapAsync List.ofSeq source

    let toResizeArrayAsync source = Internal.toResizeArrayAsync source

    let toIListAsync source = Internal.toResizeArrayAndMapAsync (fun x -> x :> IList<_>) source

    let toSeqCachedAsync source = Internal.toResizeArrayAndMapAsync (fun x -> x :> seq<_>) source

    //
    // Convert 'OfXXX' functions
    //

    let ofArray (source: 'T[]) = taskSeq {
        for c in source do
            yield c
    }

    let ofList (source: 'T list) = taskSeq {
        for c in source do
            yield c
    }

    let ofSeq (source: 'T seq) = taskSeq {
        for c in source do
            yield c
    }

    let ofResizeArray (source: 'T ResizeArray) = taskSeq {
        for c in source do
            yield c
    }

    let ofTaskSeq (source: #Task<'T> seq) = taskSeq {
        for c in source do
            let! c = c
            yield c
    }

    let ofTaskList (source: #Task<'T> list) = taskSeq {
        for c in source do
            let! c = c
            yield c
    }

    let ofTaskArray (source: #Task<'T> array) = taskSeq {
        for c in source do
            let! c = c
            yield c
    }

    let ofAsyncSeq (source: Async<'T> seq) = taskSeq {
        for c in source do
            let! c = task { return! c }
            yield c
    }

    let ofAsyncList (source: Async<'T> list) = taskSeq {
        for c in source do
            let! c = Task.ofAsync c
            yield c
    }

    let ofAsyncArray (source: Async<'T> array) = taskSeq {
        for c in source do
            let! c = Async.toTask c
            yield c
    }


    //
    // iter/map/collect functions
    //

    let iter action source = Internal.iter (SimpleAction action) source

    let iteri action source = Internal.iter (CountableAction action) source

    let iterAsync action source = Internal.iter (AsyncSimpleAction action) source

    let iteriAsync action source = Internal.iter (AsyncCountableAction action) source

    let map (mapper: 'T -> 'U) source = Internal.map (SimpleAction mapper) source

    let mapi (mapper: int -> 'T -> 'U) source = Internal.map (CountableAction mapper) source

    let mapAsync mapper source = Internal.map (AsyncSimpleAction mapper) source

    let mapiAsync mapper source = Internal.map (AsyncCountableAction mapper) source

    let collect (binder: 'T -> #IAsyncEnumerable<'U>) source = Internal.collect binder source

    let collectSeq (binder: 'T -> #seq<'U>) source = Internal.collectSeq binder source

    let collectAsync (binder: 'T -> #Task<#IAsyncEnumerable<'U>>) source : taskSeq<'U> =
        Internal.collectAsync binder source

    let collectSeqAsync (binder: 'T -> #Task<#seq<'U>>) source : taskSeq<'U> = Internal.collectSeqAsync binder source

    //
    // choosers, pickers and the like
    //

    let tryHead source = Internal.tryHead source

    let head source = task {
        match! Internal.tryHead source with
        | Some head -> return head
        | None -> return Internal.raiseEmptySeq ()
    }

    let tryLast source = Internal.tryLast source

    let last source = task {
        match! Internal.tryLast source with
        | Some last -> return last
        | None -> return Internal.raiseEmptySeq ()
    }

    let tryItem index source = Internal.tryItem index source

    let item index source = task {
        match! Internal.tryItem index source with
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

    let zip source1 source2 = Internal.zip source1 source2

    let fold folder state source = Internal.fold (FolderAction folder) state source

    let foldAsync folder state source = Internal.fold (AsyncFolderAction folder) state source
