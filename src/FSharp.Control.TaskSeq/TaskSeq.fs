namespace FSharp.Control

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

#nowarn "57"

module TaskSeq =
    // F# BUG: the following module is 'AutoOpen' and this isn't needed in the Tests project. Why do we need to open it?
    open FSharp.Control.TaskSeqBuilders

    // Just for convenience
    module Internal = TaskSeqInternal

    let empty<'T> =
        { new IAsyncEnumerable<'T> with
            member _.GetAsyncEnumerator(_) =
                { new IAsyncEnumerator<'T> with
                    member _.MoveNextAsync() = ValueTask.False
                    member _.Current = Unchecked.defaultof<'T>
                    member _.DisposeAsync() = ValueTask.CompletedTask
                }
        }

    let singleton (source: 'T) = Internal.singleton source

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

    let toArray (source: taskSeq<'T>) = [|
        let e = source.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    |]

    let toSeq (source: taskSeq<'T>) = seq {
        let e = source.GetAsyncEnumerator(CancellationToken())

        try
            while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                yield e.Current
        finally
            e.DisposeAsync().AsTask().Wait()
    }

    let toArrayAsync source =
        Internal.toResizeArrayAsync source
        |> Task.map (fun a -> a.ToArray())

    let toListAsync source = Internal.toResizeArrayAndMapAsync List.ofSeq source

    let toResizeArrayAsync source = Internal.toResizeArrayAsync source

    let toIListAsync source = Internal.toResizeArrayAndMapAsync (fun x -> x :> IList<_>) source

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
    // Utility functions
    //

    let length source = Internal.lengthBy None source
    let lengthOrMax max source = Internal.lengthBeforeMax max source
    let lengthBy predicate source = Internal.lengthBy (Some(Predicate predicate)) source
    let lengthByAsync predicate source = Internal.lengthBy (Some(PredicateAsync predicate)) source
    let init count initializer = Internal.init (Some count) (InitAction initializer)
    let initInfinite initializer = Internal.init None (InitAction initializer)
    let initAsync count initializer = Internal.init (Some count) (InitActionAsync initializer)
    let initInfiniteAsync initializer = Internal.init None (InitActionAsync initializer)

    let delay (generator: unit -> taskSeq<'T>) =
        { new IAsyncEnumerable<'T> with
            member _.GetAsyncEnumerator(ct) = generator().GetAsyncEnumerator(ct)
        }

    let concat (sources: taskSeq<#taskSeq<'T>>) = taskSeq {
        for ts in sources do
            yield! (ts :> taskSeq<'T>)
    }

    let append (source1: #taskSeq<'T>) (source2: #taskSeq<'T>) = taskSeq {
        yield! (source1 :> IAsyncEnumerable<'T>)
        yield! (source2 :> IAsyncEnumerable<'T>)
    }

    let appendSeq (source1: #taskSeq<'T>) (source2: #seq<'T>) = taskSeq {
        yield! (source1 :> IAsyncEnumerable<'T>)
        yield! (source2 :> seq<'T>)
    }

    let prependSeq (source1: #seq<'T>) (source2: #taskSeq<'T>) = taskSeq {
        yield! (source1 :> seq<'T>)
        yield! (source2 :> IAsyncEnumerable<'T>)
    }

    //
    // iter/map/collect functions
    //

    let cast source : taskSeq<'T> = Internal.map (SimpleAction(fun (x: obj) -> x :?> 'T)) source
    let box source = Internal.map (SimpleAction(fun x -> box x)) source

    let unbox<'U when 'U: struct> (source: taskSeq<obj>) : taskSeq<'U> =
        Internal.map (SimpleAction(fun x -> unbox x)) source

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

    let tryTail source = Internal.tryTail source

    let tail source = task {
        match! Internal.tryTail source with
        | Some result -> return result
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

    let indexed (source: taskSeq<'T>) = taskSeq {
        let mutable i = 0

        for x in source do
            yield i, x
            i <- i + 1
    }

    let choose chooser source = Internal.choose (TryPick chooser) source
    let chooseAsync chooser source = Internal.choose (TryPickAsync chooser) source
    let filter predicate source = Internal.filter (Predicate predicate) source
    let filterAsync predicate source = Internal.filter (PredicateAsync predicate) source
    let tryPick chooser source = Internal.tryPick (TryPick chooser) source
    let tryPickAsync chooser source = Internal.tryPick (TryPickAsync chooser) source
    let tryFind predicate source = Internal.tryFind (Predicate predicate) source
    let tryFindAsync predicate source = Internal.tryFind (PredicateAsync predicate) source
    let tryFindIndex predicate source = Internal.tryFindIndex (Predicate predicate) source
    let tryFindIndexAsync predicate source = Internal.tryFindIndex (PredicateAsync predicate) source
    let except itemsToExclude source = Internal.except itemsToExclude source
    let exceptOfSeq itemsToExclude source = Internal.exceptOfSeq itemsToExclude source

    let exists predicate source =
        Internal.tryFind (Predicate predicate) source
        |> Task.map (Option.isSome)

    let existsAsync predicate source =
        Internal.tryFind (PredicateAsync predicate) source
        |> Task.map (Option.isSome)

    let contains value source =
        Internal.tryFind (Predicate((=) value)) source
        |> Task.map (Option.isSome)

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
        match! Internal.tryFind (Predicate predicate) source with
        | Some item -> return item
        | None -> return Internal.raiseNotFound ()
    }


    let findAsync predicate source = task {
        match! Internal.tryFind (PredicateAsync predicate) source with
        | Some item -> return item
        | None -> return Internal.raiseNotFound ()
    }

    let findIndex predicate source = task {
        match! Internal.tryFindIndex (Predicate predicate) source with
        | Some item -> return item
        | None -> return Internal.raiseNotFound ()
    }

    let findIndexAsync predicate source = task {
        match! Internal.tryFindIndex (PredicateAsync predicate) source with
        | Some item -> return item
        | None -> return Internal.raiseNotFound ()
    }



    //
    // zip/unzip etc functions
    //

    let zip source1 source2 = Internal.zip source1 source2

    let fold folder state source = Internal.fold (FolderAction folder) state source

    let foldAsync folder state source = Internal.fold (AsyncFolderAction folder) state source

#nowarn "1204"
#nowarn "3513"


[<AutoOpen>]
module AsyncSeqExtensions =

    let rec WhileDynamic
        (
            sm: byref<TaskStateMachine<'Data>>,
            condition: unit -> ValueTask<bool>,
            body: TaskCode<'Data, unit>
        ) : bool =
        let vt = condition ()
        TaskBuilderBase.BindDynamic(&sm, vt, fun result ->
            TaskCode<_,_>(fun sm ->
                if result then
                    if body.Invoke(&sm) then
                        WhileDynamic(&sm, condition, body)
                    else
                        let rf = sm.ResumptionDynamicInfo.ResumptionFunc

                        sm.ResumptionDynamicInfo.ResumptionFunc <-
                            (TaskResumptionFunc<'Data>(fun sm -> WhileBodyDynamicAux(&sm, condition, body, rf)))

                        false
                else
                    true
            )
        )


    and WhileBodyDynamicAux
        (
            sm: byref<TaskStateMachine<'Data>>,
            condition: unit -> ValueTask<bool>,
            body: TaskCode<'Data, unit>,
            rf: TaskResumptionFunc<_>
        ) : bool =
        if rf.Invoke(&sm) then
            WhileDynamic(&sm, condition, body)
        else
            let rf = sm.ResumptionDynamicInfo.ResumptionFunc

            sm.ResumptionDynamicInfo.ResumptionFunc <-
                (TaskResumptionFunc<'Data>(fun sm -> WhileBodyDynamicAux(&sm, condition, body, rf)))

            false
    open Microsoft.FSharp.Core.CompilerServices

    // Add asynchronous for loop to the 'async' computation builder
    type Microsoft.FSharp.Control.AsyncBuilder with

        member x.For(tasksq: IAsyncEnumerable<'T>, action: 'T -> Async<unit>) =
            tasksq
            |> TaskSeq.iterAsync (action >> Async.StartAsTask)
            |> Async.AwaitTask

    // Add asynchronous for loop to the 'task' computation builder
    type Microsoft.FSharp.Control.TaskBuilder with


        member inline this.While(condition : unit -> ValueTask<bool>,  body  : TaskCode<'TOverall,unit>) =
            TaskCode<_,_>(fun sm ->
                WhileDynamic(&sm, condition, body)

            )



        member inline this.For
            (
                tasksq: IAsyncEnumerable<'T>,
                body: 'T -> TaskCode<'TOverall, unit>
            ) : TaskCode<'TOverall, unit> =
            TaskCode<'TOverall, unit>(fun sm ->

                this
                    .Using(
                        tasksq.GetAsyncEnumerator(CancellationToken()),
                        (fun e ->
                            let next () = e.MoveNextAsync()
                            this.While(next, (fun sm -> (body e.Current).Invoke(&sm))))
                    )
                    .Invoke(&sm))

    let foo () =
        task {
            let mutable sum = 0
            let xs = taskSeq { 1; 2; 3}
            for x in xs do
                sum <- sum + x
        }
