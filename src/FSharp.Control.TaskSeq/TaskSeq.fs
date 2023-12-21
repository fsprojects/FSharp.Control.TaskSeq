namespace FSharp.Control

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

#nowarn "57"

// Just for convenience
module Internal = TaskSeqInternal

[<AutoOpen>]
module TaskSeqExtensions =
    // these need to be in a module, not a type for proper auto-initialization of generic values
    module TaskSeq =
        let empty<'T> = Internal.empty<'T>


[<Sealed; AbstractClass>]
type TaskSeq private () =
    // Rules for static classes, see bug report: https://github.com/dotnet/fsharp/issues/8093
    // F# does not need this internally, but C# does
    // 'Abstract & Sealed': makes it a static class in C#
    // the 'private ()' ensure that a constructor is emitted, which is required by IL

    static member singleton(value: 'T) = Internal.singleton value

    static member isEmpty source = Internal.isEmpty source

    //
    // Convert 'ToXXX' functions
    //

    static member toList(source: TaskSeq<'T>) = [
        Internal.checkNonNull (nameof source) source
        let e = source.GetAsyncEnumerator CancellationToken.None

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    ]

    static member toArray(source: TaskSeq<'T>) = [|
        Internal.checkNonNull (nameof source) source
        let e = source.GetAsyncEnumerator CancellationToken.None

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    |]

    static member toSeq(source: TaskSeq<'T>) =
        Internal.checkNonNull (nameof source) source

        seq {

            let e = source.GetAsyncEnumerator CancellationToken.None

            try
                while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                    yield e.Current
            finally
                e.DisposeAsync().AsTask().Wait()
        }

    static member toArrayAsync source =
        Internal.toResizeArrayAsync source
        |> Task.map (fun a -> a.ToArray())

    static member toListAsync source = Internal.toResizeArrayAndMapAsync List.ofSeq source

    static member toResizeArrayAsync source = Internal.toResizeArrayAsync source

    static member toIListAsync source = Internal.toResizeArrayAndMapAsync (fun x -> x :> IList<_>) source

    //
    // Convert 'OfXXX' functions
    //

    static member ofArray(source: 'T[]) =
        Internal.checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                yield c
        }

    static member ofList(source: 'T list) = taskSeq {
        for c in source do
            yield c
    }

    static member ofSeq(source: 'T seq) =
        Internal.checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                yield c
        }

    static member ofResizeArray(source: 'T ResizeArray) =
        Internal.checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                yield c
        }

    static member ofTaskSeq(source: #Task<'T> seq) =
        Internal.checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                let! c = c
                yield c
        }

    static member ofTaskList(source: #Task<'T> list) = taskSeq {
        for c in source do
            let! c = c
            yield c
    }

    static member ofTaskArray(source: #Task<'T> array) =
        Internal.checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                let! c = c
                yield c
        }

    static member ofAsyncSeq(source: Async<'T> seq) =
        Internal.checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                let! c = task { return! c }
                yield c
        }

    static member ofAsyncList(source: Async<'T> list) = taskSeq {
        for c in source do
            let! c = Task.ofAsync c
            yield c
    }

    static member ofAsyncArray(source: Async<'T> array) =
        Internal.checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                let! c = Async.toTask c
                yield c
        }

    //
    // Utility functions
    //

    static member length source = Internal.lengthBy None source
    static member lengthOrMax max source = Internal.lengthBeforeMax max source
    static member lengthBy predicate source = Internal.lengthBy (Some(Predicate predicate)) source
    static member lengthByAsync predicate source = Internal.lengthBy (Some(PredicateAsync predicate)) source
    static member init count initializer = Internal.init (Some count) (InitAction initializer)
    static member initInfinite initializer = Internal.init None (InitAction initializer)
    static member initAsync count initializer = Internal.init (Some count) (InitActionAsync initializer)
    static member initInfiniteAsync initializer = Internal.init None (InitActionAsync initializer)

    static member delay(generator: unit -> TaskSeq<'T>) =
        { new IAsyncEnumerable<'T> with
            member _.GetAsyncEnumerator(ct) = generator().GetAsyncEnumerator(ct)
        }

    static member concat(sources: TaskSeq<#TaskSeq<'T>>) =
        Internal.checkNonNull (nameof sources) sources

        taskSeq {
            for ts in sources do
                // no null-check of inner taskseqs, similar to seq
                yield! (ts :> TaskSeq<'T>)
        }

    static member append (source1: TaskSeq<'T>) (source2: TaskSeq<'T>) =
        Internal.checkNonNull (nameof source1) source1
        Internal.checkNonNull (nameof source2) source2

        taskSeq {
            yield! source1
            yield! source2
        }

    static member appendSeq (source1: TaskSeq<'T>) (source2: seq<'T>) =
        Internal.checkNonNull (nameof source1) source1
        Internal.checkNonNull (nameof source2) source2

        taskSeq {
            yield! source1
            yield! source2
        }

    static member prependSeq (source1: seq<'T>) (source2: TaskSeq<'T>) =
        Internal.checkNonNull (nameof source1) source1
        Internal.checkNonNull (nameof source2) source2

        taskSeq {
            yield! source1
            yield! source2
        }

    //
    // iter/map/collect functions
    //

    static member cast source : TaskSeq<'T> = Internal.map (SimpleAction(fun (x: obj) -> x :?> 'T)) source
    static member box source = Internal.map (SimpleAction box) source
    static member unbox<'U when 'U: struct>(source: TaskSeq<obj>) : TaskSeq<'U> = Internal.map (SimpleAction unbox) source
    static member iter action source = Internal.iter (SimpleAction action) source
    static member iteri action source = Internal.iter (CountableAction action) source
    static member iterAsync action source = Internal.iter (AsyncSimpleAction action) source
    static member iteriAsync action source = Internal.iter (AsyncCountableAction action) source
    static member map (mapper: 'T -> 'U) source = Internal.map (SimpleAction mapper) source
    static member mapi (mapper: int -> 'T -> 'U) source = Internal.map (CountableAction mapper) source
    static member mapAsync mapper source = Internal.map (AsyncSimpleAction mapper) source
    static member mapiAsync mapper source = Internal.map (AsyncCountableAction mapper) source
    static member collect (binder: 'T -> #TaskSeq<'U>) source = Internal.collect binder source
    static member collectSeq (binder: 'T -> #seq<'U>) source = Internal.collectSeq binder source
    static member collectAsync (binder: 'T -> #Task<#TaskSeq<'U>>) source : TaskSeq<'U> = Internal.collectAsync binder source
    static member collectSeqAsync (binder: 'T -> #Task<#seq<'U>>) source : TaskSeq<'U> = Internal.collectSeqAsync binder source

    //
    // choosers, pickers and the like
    //

    static member tryHead source = Internal.tryHead source

    static member head source =
        Internal.tryHead source
        |> Task.map (Option.defaultWith Internal.raiseEmptySeq)

    static member tryLast source = Internal.tryLast source

    static member last source =
        Internal.tryLast source
        |> Task.map (Option.defaultWith Internal.raiseEmptySeq)

    static member tryTail source = Internal.tryTail source

    static member tail source =
        Internal.tryTail source
        |> Task.map (Option.defaultWith Internal.raiseEmptySeq)

    static member tryItem index source = Internal.tryItem index source

    static member item index source =
        if index < 0 then
            invalidArg (nameof index) "The input must be non-negative."

        Internal.tryItem index source
        |> Task.map (Option.defaultWith Internal.raiseInsufficient)

    static member tryExactlyOne source = Internal.tryExactlyOne source

    static member exactlyOne source =
        Internal.tryExactlyOne source
        |> Task.map (Option.defaultWith (fun () -> invalidArg (nameof source) "The input sequence contains more than one element."))

    static member indexed(source: TaskSeq<'T>) =
        Internal.checkNonNull (nameof source) source

        taskSeq {
            let mutable i = 0

            for x in source do
                yield i, x
                i <- i + 1
        }

    static member choose chooser source = Internal.choose (TryPick chooser) source
    static member chooseAsync chooser source = Internal.choose (TryPickAsync chooser) source

    static member filter predicate source = Internal.filter (Predicate predicate) source
    static member filterAsync predicate source = Internal.filter (PredicateAsync predicate) source
    static member where predicate source = Internal.filter (Predicate predicate) source
    static member whereAsync predicate source = Internal.filter (PredicateAsync predicate) source

    static member skip count source = Internal.skipOrTake Skip count source
    static member drop count source = Internal.skipOrTake Drop count source
    static member take count source = Internal.skipOrTake Take count source
    static member truncate count source = Internal.skipOrTake Truncate count source

    static member takeWhile predicate source = Internal.takeWhile Exclusive (Predicate predicate) source
    static member takeWhileAsync predicate source = Internal.takeWhile Exclusive (PredicateAsync predicate) source
    static member takeWhileInclusive predicate source = Internal.takeWhile Inclusive (Predicate predicate) source
    static member takeWhileInclusiveAsync predicate source = Internal.takeWhile Inclusive (PredicateAsync predicate) source
    static member skipWhile predicate source = Internal.skipWhile Exclusive (Predicate predicate) source
    static member skipWhileAsync predicate source = Internal.skipWhile Exclusive (PredicateAsync predicate) source
    static member skipWhileInclusive predicate source = Internal.skipWhile Inclusive (Predicate predicate) source
    static member skipWhileInclusiveAsync predicate source = Internal.skipWhile Inclusive (PredicateAsync predicate) source

    static member tryPick chooser source = Internal.tryPick (TryPick chooser) source
    static member tryPickAsync chooser source = Internal.tryPick (TryPickAsync chooser) source
    static member tryFind predicate source = Internal.tryFind (Predicate predicate) source
    static member tryFindAsync predicate source = Internal.tryFind (PredicateAsync predicate) source
    static member tryFindIndex predicate source = Internal.tryFindIndex (Predicate predicate) source
    static member tryFindIndexAsync predicate source = Internal.tryFindIndex (PredicateAsync predicate) source

    static member except itemsToExclude source = Internal.except itemsToExclude source
    static member exceptOfSeq itemsToExclude source = Internal.exceptOfSeq itemsToExclude source

    static member exists predicate source =
        Internal.tryFind (Predicate predicate) source
        |> Task.map (Option.isSome)

    static member existsAsync predicate source =
        Internal.tryFind (PredicateAsync predicate) source
        |> Task.map (Option.isSome)

    static member contains value source =
        Internal.tryFind (Predicate((=) value)) source
        |> Task.map (Option.isSome)

    static member pick chooser source =
        Internal.tryPick (TryPick chooser) source
        |> Task.map (Option.defaultWith Internal.raiseNotFound)

    static member pickAsync chooser source =
        Internal.tryPick (TryPickAsync chooser) source
        |> Task.map (Option.defaultWith Internal.raiseNotFound)

    static member find predicate source =
        Internal.tryFind (Predicate predicate) source
        |> Task.map (Option.defaultWith Internal.raiseNotFound)

    static member findAsync predicate source =
        Internal.tryFind (PredicateAsync predicate) source
        |> Task.map (Option.defaultWith Internal.raiseNotFound)

    static member findIndex predicate source =
        Internal.tryFindIndex (Predicate predicate) source
        |> Task.map (Option.defaultWith Internal.raiseNotFound)

    static member findIndexAsync predicate source =
        Internal.tryFindIndex (PredicateAsync predicate) source
        |> Task.map (Option.defaultWith Internal.raiseNotFound)



    //
    // zip/unzip/fold etc functions
    //

    static member zip source1 source2 = Internal.zip source1 source2
    static member fold folder state source = Internal.fold (FolderAction folder) state source
    static member foldAsync folder state source = Internal.fold (AsyncFolderAction folder) state source
