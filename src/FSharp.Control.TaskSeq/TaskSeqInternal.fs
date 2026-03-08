namespace FSharp.Control

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

[<Struct>]
type internal AsyncEnumStatus =
    | BeforeAll
    | WithCurrent
    | AfterAll

[<Struct>]
type internal TakeOrSkipKind =
    /// use the Seq.take semantics, raises exception if not enough elements
    | Take
    /// use the Seq.skip semantics, raises exception if not enough elements
    | Skip
    /// use the Seq.truncate semantics, safe operation, returns all if count exceeds the seq
    | Truncate
    /// no Seq equiv, but like Stream.drop in Scala: safe operation, return empty if not enough elements
    | Drop

[<Struct>]
type internal Action<'T, 'U, 'TaskU when 'TaskU :> Task<'U>> =
    | CountableAction of countable_action: (int -> 'T -> 'U)
    | SimpleAction of simple_action: ('T -> 'U)
    | AsyncCountableAction of async_countable_action: (int -> 'T -> 'TaskU)
    | AsyncSimpleAction of async_simple_action: ('T -> 'TaskU)

[<Struct>]
type internal FolderAction<'T, 'State, 'TaskState when 'TaskState :> Task<'State>> =
    | FolderAction of state_action: ('State -> 'T -> 'State)
    | AsyncFolderAction of async_state_action: ('State -> 'T -> 'TaskState)

[<Struct>]
type internal ChooserAction<'T, 'U, 'TaskOption when 'TaskOption :> Task<'U option>> =
    | TryPick of try_pick: ('T -> 'U option)
    | TryPickAsync of async_try_pick: ('T -> 'TaskOption)

[<Struct>]
type internal PredicateAction<'T, 'TaskBool when 'TaskBool :> Task<bool>> =
    | Predicate of try_filter: ('T -> bool)
    | PredicateAsync of async_try_filter: ('T -> 'TaskBool)

[<Struct>]
type internal InitAction<'T, 'TaskT when 'TaskT :> Task<'T>> =
    | InitAction of init_item: (int -> 'T)
    | InitActionAsync of async_init_item: (int -> 'TaskT)

[<Struct>]
type internal ProjectorAction<'T, 'Key, 'TaskKey when 'TaskKey :> Task<'Key>> =
    | ProjectorAction of projector: ('T -> 'Key)
    | AsyncProjectorAction of async_projector: ('T -> 'TaskKey)

[<Struct>]
type internal MapFolderAction<'T, 'State, 'Result, 'TaskResultState when 'TaskResultState :> Task<'Result * 'State>> =
    | MapFolderAction of map_folder_action: ('State -> 'T -> 'Result * 'State)
    | AsyncMapFolderAction of async_map_folder_action: ('State -> 'T -> 'TaskResultState)

[<Struct>]
type internal ManyOrOne<'T> =
    | Many of source_seq: TaskSeq<'T>
    | One of source_item: 'T

module internal TaskSeqInternal =
    /// Raise an NRE for arguments that are null. Only used for 'source' parameters, never for function parameters.
    let inline checkNonNull argName arg =
        if isNull arg then
            nullArg argName

    let inline raiseEmptySeq () = invalidArg "source" "The input task sequence was empty."

    /// Moves the enumerator to its first element, assuming it has just been allocated.
    /// Raises "The input sequence was empty" if there was no first element.
    let inline moveFirstOrRaiseUnsafe (e: IAsyncEnumerator<_>) = task {
        let! hasFirst = e.MoveNextAsync()

        if not hasFirst then
            invalidArg "source" "The input task sequence was empty."
    }

    /// Tests the given integer value and raises if it is -1 or lower.
    let inline raiseCannotBeNegative name value =
        if value >= 0 then
            ()
        else
            invalidArg name $"The value must be non-negative, but was {value}."

    let inline raiseOutOfBounds name =
        invalidArg name "The value or index must be within the bounds of the task sequence."

    let inline raiseInsufficient () =
        // this is correct, it is NOT an InvalidOperationException (see Seq.fs in F# Core)
        // but instead, it's an ArgumentException... FWIW lol
        invalidArg "source" "The input task sequence was has an insufficient number of elements."

    let inline raiseNotFound () =
        KeyNotFoundException("The predicate function or index did not satisfy any item in the task sequence.")
        |> raise

    let isEmpty (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! step = e.MoveNextAsync()
            return not step
        }

    let empty<'T> =
        { new IAsyncEnumerable<'T> with
            member _.GetAsyncEnumerator _ =
                { new IAsyncEnumerator<'T> with
                    member _.MoveNextAsync() = ValueTask.False
                    member _.Current = Unchecked.defaultof<'T>
                    member _.DisposeAsync() = ValueTask.CompletedTask
                }
        }

    let singleton (value: 'T) =
        { new IAsyncEnumerable<'T> with
            member _.GetAsyncEnumerator _ =
                let mutable status = BeforeAll

                { new IAsyncEnumerator<'T> with
                    member _.MoveNextAsync() =
                        match status with
                        | BeforeAll ->
                            status <- WithCurrent
                            ValueTask.True
                        | WithCurrent ->
                            status <- AfterAll
                            ValueTask.False
                        | AfterAll -> ValueTask.False

                    member _.Current: 'T =
                        match status with
                        | WithCurrent -> value
                        | _ -> Unchecked.defaultof<'T>

                    member _.DisposeAsync() = ValueTask.CompletedTask
                }
        }

    /// Returns length unconditionally, or based on a predicate
    let lengthBy predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {

            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let mutable i = 0
            let! step = e.MoveNextAsync()
            go <- step

            match predicate with
            | None ->
                while go do
                    let! step = e.MoveNextAsync()
                    i <- i + 1 // update before moving: we are counting, not indexing
                    go <- step

            | Some(Predicate predicate) ->
                while go do
                    if predicate e.Current then
                        i <- i + 1

                    let! step = e.MoveNextAsync()
                    go <- step

            | Some(PredicateAsync predicate) ->
                while go do
                    match! predicate e.Current with
                    | true -> i <- i + 1
                    | false -> ()

                    let! step = e.MoveNextAsync()
                    go <- step

            return i
        }

    /// Returns length unconditionally, or based on a predicate
    let lengthBeforeMax max (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let mutable i = 0
            let! step = e.MoveNextAsync()
            go <- step

            while go && i < max do
                i <- i + 1 // update before moving: we are counting, not indexing
                let! step = e.MoveNextAsync()
                go <- step

            return i
        }

    let inline maxMin ([<InlineIfLambda>] maxOrMin) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            do! moveFirstOrRaiseUnsafe e

            let mutable acc = e.Current

            while! e.MoveNextAsync() do
                acc <- maxOrMin e.Current acc

            return acc
        }

    // 'compare' is either `<` or `>` (i.e, less-than, greater-than resp.)
    let inline maxMinBy ([<InlineIfLambda>] compare) ([<InlineIfLambda>] projection) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            do! moveFirstOrRaiseUnsafe e

            let value = e.Current
            let mutable accProjection = projection value
            let mutable accValue = value

            while! e.MoveNextAsync() do
                let value = e.Current
                let currentProjection = projection value

                if compare accProjection currentProjection then
                    accProjection <- currentProjection
                    accValue <- value

            return accValue
        }

    // 'compare' is either `<` or `>` (i.e, less-than, greater-than resp.)
    let inline maxMinByAsync ([<InlineIfLambda>] compare) ([<InlineIfLambda>] projectionAsync) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            do! moveFirstOrRaiseUnsafe e

            let value = e.Current
            let! projValue = projectionAsync value
            let mutable accProjection = projValue
            let mutable accValue = value

            while! e.MoveNextAsync() do
                let value = e.Current
                let! currentProjection = projectionAsync value

                if compare accProjection currentProjection then
                    accProjection <- currentProjection
                    accValue <- value

            return accValue
        }

    let tryExactlyOne (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            match! e.MoveNextAsync() with
            | true ->
                // grab first item and test if there's a second item
                let current = e.Current

                match! e.MoveNextAsync() with
                | true -> return None // 2 or more items
                | false -> return Some current // exactly one

            | false ->
                // zero items
                return None
        }


    let init count initializer = taskSeq {
        let mutable i = 0

        let count =
            match count with
            | Some c ->
                raiseCannotBeNegative (nameof count) c
                c

            | None -> Int32.MaxValue

        match initializer with
        | InitAction init ->
            while i < count do
                yield init i
                i <- i + 1

        | InitActionAsync asyncInit ->
            while i < count do
                let! result = asyncInit i
                yield result
                i <- i + 1

    }

    let unfold generator state = taskSeq {
        let mutable go = true
        let mutable currentState = state

        while go do
            match generator currentState with
            | None -> go <- false
            | Some(value, nextState) ->
                yield value
                currentState <- nextState
    }

    let unfoldAsync generator state = taskSeq {
        let mutable go = true
        let mutable currentState = state

        while go do
            let! result = (generator currentState: Task<_>)

            match result with
            | None -> go <- false
            | Some(value, nextState) ->
                yield value
                currentState <- nextState
    }

    let iter action (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step = e.MoveNextAsync()
            go <- step

            // this ensures that the inner loop is optimized for the closure
            // though perhaps we need to split into individual functions after all to use
            // InlineIfLambda?
            match action with
            | CountableAction action ->
                let mutable i = 0

                while go do
                    do action i e.Current
                    let! step = e.MoveNextAsync()
                    i <- i + 1
                    go <- step

            | SimpleAction action ->
                while go do
                    do action e.Current
                    let! step = e.MoveNextAsync()
                    go <- step

            | AsyncCountableAction action ->
                let mutable i = 0

                while go do
                    do! action i e.Current
                    let! step = e.MoveNextAsync()
                    i <- i + 1
                    go <- step

            | AsyncSimpleAction action ->
                while go do
                    do! action e.Current
                    let! step = e.MoveNextAsync()
                    go <- step
        }

    let fold folder initial (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let mutable result = initial
            let! step = e.MoveNextAsync()
            go <- step

            match folder with
            | FolderAction folder ->
                while go do
                    result <- folder result e.Current
                    let! step = e.MoveNextAsync()
                    go <- step

            | AsyncFolderAction folder ->
                while go do
                    let! tempResult = folder result e.Current
                    result <- tempResult
                    let! step = e.MoveNextAsync()
                    go <- step

            return result
        }

    let scan folder initial (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        match folder with
        | FolderAction folder -> taskSeq {
            let mutable state = initial
            yield state

            for item in source do
                state <- folder state item
                yield state
          }

        | AsyncFolderAction folder -> taskSeq {
            let mutable state = initial
            yield state

            for item in source do
                let! newState = folder state item
                state <- newState
                yield state
          }

    let reduce folder (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! hasFirst = e.MoveNextAsync()

            if not hasFirst then
                raiseEmptySeq ()

            let mutable result = e.Current
            let! step = e.MoveNextAsync()
            let mutable go = step

            match folder with
            | FolderAction folder ->
                while go do
                    result <- folder result e.Current
                    let! step = e.MoveNextAsync()
                    go <- step

            | AsyncFolderAction folder ->
                while go do
                    let! tempResult = folder result e.Current
                    result <- tempResult
                    let! step = e.MoveNextAsync()
                    go <- step

            return result
        }

    let mapFold (folder: MapFolderAction<_, _, _, _>) initial (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let mutable state = initial
            let results = ResizeArray()
            let! step = e.MoveNextAsync()
            go <- step

            match folder with
            | MapFolderAction folder ->
                while go do
                    let result, newState = folder state e.Current
                    results.Add result
                    state <- newState
                    let! step = e.MoveNextAsync()
                    go <- step

            | AsyncMapFolderAction folder ->
                while go do
                    let! (result, newState) = folder state e.Current
                    results.Add result
                    state <- newState
                    let! step = e.MoveNextAsync()
                    go <- step

            return results.ToArray(), state
        }

    let toResizeArrayAsync source =
        checkNonNull (nameof source) source

        task {
            let res = ResizeArray()
            do! source |> iter (SimpleAction(fun item -> res.Add item))
            return res
        }

    let toResizeArrayAndMapAsync mapper source = (toResizeArrayAsync >> Task.map mapper) source

    let map mapper (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        match mapper with
        | CountableAction mapper -> taskSeq {
            let mutable i = 0

            for c in source do
                yield mapper i c
                i <- i + 1
          }

        | SimpleAction mapper -> taskSeq {
            for c in source do
                yield mapper c
          }

        | AsyncCountableAction mapper -> taskSeq {
            let mutable i = 0

            for c in source do
                let! result = mapper i c
                yield result
                i <- i + 1
          }

        | AsyncSimpleAction mapper -> taskSeq {
            for c in source do
                let! result = mapper c
                yield result
          }

    let zip (source1: TaskSeq<_>) (source2: TaskSeq<_>) =
        checkNonNull (nameof source1) source1
        checkNonNull (nameof source2) source2

        taskSeq {
            use e1 = source1.GetAsyncEnumerator CancellationToken.None
            use e2 = source2.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step1 = e1.MoveNextAsync()
            let! step2 = e2.MoveNextAsync()
            go <- step1 && step2

            while go do
                yield e1.Current, e2.Current
                let! step1 = e1.MoveNextAsync()
                let! step2 = e2.MoveNextAsync()
                go <- step1 && step2
        }

    let collect (binder: _ -> #IAsyncEnumerable<_>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                yield! binder c :> IAsyncEnumerable<_>
        }

    let collectSeq (binder: _ -> #seq<_>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                yield! binder c :> seq<_>
        }

    let collectAsync (binder: _ -> #Task<#IAsyncEnumerable<_>>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                let! result = binder c
                yield! result :> IAsyncEnumerable<_>
        }

    let collectSeqAsync (binder: _ -> #Task<#seq<_>>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                let! result = binder c
                yield! result :> seq<_>
        }

    let tryLast (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let mutable last = ValueNone
            let! step = e.MoveNextAsync()
            go <- step

            while go do
                last <- ValueSome e.Current
                let! step = e.MoveNextAsync()
                go <- step

            match last with
            | ValueSome value -> return Some value
            | ValueNone -> return None
        }

    let tryHead (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            match! e.MoveNextAsync() with
            | true -> return Some e.Current
            | false -> return None
        }

    let tryTail (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            match! e.MoveNextAsync() with
            | false -> return None
            | true ->
                return
                    taskSeq {
                        let mutable go = true
                        let! step = e.MoveNextAsync()
                        go <- step

                        while go do
                            yield e.Current
                            let! step = e.MoveNextAsync()
                            go <- step
                    }
                    |> Some
        }

    let tryItem index (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            if index < 0 then
                // while the loop below wouldn't run anyway, we don't want to call MoveNext in this case
                // to prevent side effects hitting unnecessarily
                return None
            else
                use e = source.GetAsyncEnumerator CancellationToken.None
                let mutable go = true
                let mutable idx = 0
                let mutable foundItem = None
                let! step = e.MoveNextAsync()
                go <- step

                while go && idx <= index do
                    if idx = index then
                        foundItem <- Some e.Current
                        go <- false
                    else
                        let! step = e.MoveNextAsync()
                        go <- step
                        idx <- idx + 1

                return foundItem
        }

    let tryPick chooser (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            let mutable go = true
            let mutable foundItem = None
            let! step = e.MoveNextAsync()
            go <- step

            match chooser with
            | TryPick picker ->
                while go do
                    match picker e.Current with
                    | Some value ->
                        foundItem <- Some value
                        go <- false
                    | None ->
                        let! step = e.MoveNextAsync()
                        go <- step

            | TryPickAsync picker ->
                while go do
                    match! picker e.Current with
                    | Some value ->
                        foundItem <- Some value
                        go <- false
                    | None ->
                        let! step = e.MoveNextAsync()
                        go <- step

            return foundItem
        }

    let tryFind predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            let mutable go = true
            let mutable foundItem = None
            let! step = e.MoveNextAsync()
            go <- step

            match predicate with
            | Predicate predicate ->
                while go do
                    let current = e.Current

                    match predicate current with
                    | true ->
                        foundItem <- Some current
                        go <- false
                    | false ->
                        let! step = e.MoveNextAsync()
                        go <- step

            | PredicateAsync predicate ->
                while go do
                    let current = e.Current

                    match! predicate current with
                    | true ->
                        foundItem <- Some current
                        go <- false
                    | false ->
                        let! step = e.MoveNextAsync()
                        go <- step

            return foundItem
        }

    let tryFindIndex predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            let mutable go = true
            let mutable isFound = false
            let mutable index = -1
            let! step = e.MoveNextAsync()
            go <- step

            match predicate with
            | Predicate predicate ->
                while go && not isFound do
                    index <- index + 1
                    isFound <- predicate e.Current

                    if not isFound then
                        let! step = e.MoveNextAsync()
                        go <- step

            | PredicateAsync predicate ->
                while go && not isFound do
                    index <- index + 1
                    let! predicateResult = predicate e.Current
                    isFound <- predicateResult

                    if not isFound then
                        let! step = e.MoveNextAsync()
                        go <- step

            if isFound then return Some index else return None
        }

    let choose chooser (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {

            match chooser with
            | TryPick picker ->
                for item in source do
                    match picker item with
                    | Some value -> yield value
                    | None -> ()

            | TryPickAsync picker ->
                for item in source do
                    match! picker item with
                    | Some value -> yield value
                    | None -> ()
        }

    let filter predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            match predicate with
            | Predicate syncPredicate ->
                for item in source do
                    if syncPredicate item then
                        yield item

            | PredicateAsync asyncPredicate ->
                for item in source do
                    match! asyncPredicate item with
                    | true -> yield item
                    | false -> ()
        }

    let forall predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        match predicate with
        | Predicate syncPredicate -> task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable state = true
            let! cont = e.MoveNextAsync()
            let mutable hasMore = cont

            while state && hasMore do
                state <- syncPredicate e.Current

                if state then
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            return state
          }

        | PredicateAsync asyncPredicate -> task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable state = true
            let! cont = e.MoveNextAsync()
            let mutable hasMore = cont

            while state && hasMore do
                let! pred = asyncPredicate e.Current
                state <- pred

                if state then
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            return state
          }

    /// Direct bool-returning exists, avoiding the Option<'T> allocation that tryFind+isSome would incur.
    let exists predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        match predicate with
        | Predicate syncPredicate -> task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable found = false
            let! cont = e.MoveNextAsync()
            let mutable hasMore = cont

            while not found && hasMore do
                found <- syncPredicate e.Current

                if not found then
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            return found
          }

        | PredicateAsync asyncPredicate -> task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable found = false
            let! cont = e.MoveNextAsync()
            let mutable hasMore = cont

            while not found && hasMore do
                let! pred = asyncPredicate e.Current
                found <- pred

                if not found then
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            return found
          }

    /// Direct bool-returning contains, avoiding the Option<'T> allocation and closure that tryFind+isSome would incur.
    let contains (value: 'T) (source: TaskSeq<'T>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable found = false
            let! cont = e.MoveNextAsync()
            let mutable hasMore = cont

            while not found && hasMore do
                if e.Current = value then
                    found <- true
                else
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            return found
        }

    let distinct (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            // only create hashset when we start iterating; sequential so plain HashSet suffices
            let seen = HashSet<_>(HashIdentity.Structural)

            for item in source do
                if seen.Add item then
                    yield item
        }

    let distinctBy (projection: _ -> _) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            let seen = HashSet<_>(HashIdentity.Structural)

            for item in source do
                if seen.Add(projection item) then
                    yield item
        }

    let distinctByAsync (projection: _ -> #Task<_>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            let seen = HashSet<_>(HashIdentity.Structural)

            for item in source do
                let! key = projection item

                if seen.Add key then
                    yield item
        }

    let skipOrTake skipOrTake count (source: TaskSeq<_>) =
        checkNonNull (nameof source) source
        raiseCannotBeNegative (nameof count) count

        match skipOrTake with
        | Skip ->
            // don't create a new sequence if count = 0
            if count = 0 then
                source
            else
                taskSeq {
                    use e = source.GetAsyncEnumerator CancellationToken.None

                    for _ in 1..count do
                        let! hasMore = e.MoveNextAsync()

                        if not hasMore then
                            raiseInsufficient ()

                    while! e.MoveNextAsync() do
                        yield e.Current

                }
        | Drop ->
            // don't create a new sequence if count = 0
            if count = 0 then
                source
            else
                taskSeq {
                    use e = source.GetAsyncEnumerator CancellationToken.None

                    let! step = e.MoveNextAsync()
                    let mutable cont = step
                    let mutable pos = 0

                    // skip, or stop looping if we reached the end
                    while cont do
                        pos <- pos + 1

                        if pos < count then
                            let! moveNext = e.MoveNextAsync()
                            cont <- moveNext
                        else
                            cont <- false

                    // return the rest
                    while! e.MoveNextAsync() do
                        yield e.Current

                }
        | Take ->
            // don't initialize an empty task sequence
            if count = 0 then
                empty
            else
                taskSeq {
                    use e = source.GetAsyncEnumerator CancellationToken.None

                    for _ in count .. - 1 .. 1 do
                        let! step = e.MoveNextAsync()

                        if not step then
                            raiseInsufficient ()

                        yield e.Current
                }

        | Truncate ->
            // don't create a new sequence if count = 0
            if count = 0 then
                empty
            else
                taskSeq {
                    use e = source.GetAsyncEnumerator CancellationToken.None

                    let! step = e.MoveNextAsync()
                    let mutable cont = step
                    let mutable pos = 0

                    // return items until we've exhausted the seq
                    while cont do
                        yield e.Current
                        pos <- pos + 1

                        if pos < count then
                            let! moveNext = e.MoveNextAsync()
                            cont <- moveNext
                        else
                            cont <- false

                }

    let takeWhile isInclusive predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! notEmpty = e.MoveNextAsync()
            let mutable hasMore = notEmpty

            match predicate with
            | Predicate synchronousPredicate ->
                while hasMore && synchronousPredicate e.Current do
                    yield e.Current
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            | PredicateAsync asyncPredicate ->
                let mutable predicateHolds = true

                while hasMore && predicateHolds do // TODO: check perf if `while!` is going to be better or equal
                    let! predicateIsTrue = asyncPredicate e.Current

                    if predicateIsTrue then
                        yield e.Current
                        let! cont = e.MoveNextAsync()
                        hasMore <- cont

                    predicateHolds <- predicateIsTrue

            // "inclusive" means: always return the item that we pulled, regardless of the result of applying the predicate
            // and only stop thereafter. The non-inclusive versions, in contrast, do not return the item under which the predicate is false.
            if hasMore && isInclusive then
                yield e.Current
        }

    let skipWhile isInclusive predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! notEmpty = e.MoveNextAsync()
            let mutable hasMore = notEmpty

            match predicate with
            | Predicate synchronousPredicate ->
                while hasMore && synchronousPredicate e.Current do
                    // keep skipping
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            | PredicateAsync asyncPredicate ->
                let mutable predicateHolds = true

                while hasMore && predicateHolds do // TODO: check perf if `while!` is going to be better or equal
                    let! predicateIsTrue = asyncPredicate e.Current

                    if predicateIsTrue then
                        // keep skipping
                        let! cont = e.MoveNextAsync()
                        hasMore <- cont

                    predicateHolds <- predicateIsTrue

            // "inclusive" means: always skip the item that we pulled, regardless of the result of applying the predicate
            // and only stop thereafter. The non-inclusive versions, in contrast, do not skip the item under which the predicate is false.
            if hasMore && not isInclusive then
                yield e.Current // don't skip, unless inclusive

            // propagate the rest
            while! e.MoveNextAsync() do
                yield e.Current
        }

    /// InsertAt or InsertManyAt
    let insertAt index valueOrValues (source: TaskSeq<_>) =
        raiseCannotBeNegative (nameof index) index

        taskSeq {
            let mutable i = 0

            for item in source do
                if i = index then
                    match valueOrValues with
                    | Many values -> yield! values
                    | One value -> yield value

                yield item
                i <- i + 1

            // allow inserting at the end
            if i = index then
                match valueOrValues with
                | Many values -> yield! values
                | One value -> yield value

            if i < index then
                raiseOutOfBounds (nameof index)
        }

    let removeAt index (source: TaskSeq<'T>) =
        raiseCannotBeNegative (nameof index) index

        taskSeq {
            let mutable i = 0

            for item in source do
                if i <> index then
                    yield item

                i <- i + 1

            // cannot remove past end of sequence
            if i <= index then
                raiseOutOfBounds (nameof index)
        }

    let removeManyAt index count (source: TaskSeq<'T>) =
        raiseCannotBeNegative (nameof index) index

        taskSeq {
            let mutable i = 0
            let indexEnd = index + count

            for item in source do
                if i < index || i >= indexEnd then
                    yield item

                i <- i + 1

            // cannot remove past end of sequence
            if i <= index then
                raiseOutOfBounds (nameof index)
        }

    let updateAt index value (source: TaskSeq<'T>) =
        raiseCannotBeNegative (nameof index) index

        taskSeq {
            let mutable i = 0

            for item in source do
                if i <> index then // most common scenario on top (cpu prediction)
                    yield item
                else
                    yield value

                i <- i + 1

            // cannot update past end of sequence
            if i <= index then
                raiseOutOfBounds (nameof index)
        }

    let except (itemsToExclude: TaskSeq<_>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source
        checkNonNull (nameof itemsToExclude) itemsToExclude

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step = e.MoveNextAsync()
            go <- step

            if step then
                // only create hashset by the time we actually start iterating;
                // taskSeq enumerates sequentially, so a plain HashSet suffices — no locking needed.
                let hashSet = HashSet<_>(HashIdentity.Structural)

                use excl = itemsToExclude.GetAsyncEnumerator CancellationToken.None
                let! exclStep = excl.MoveNextAsync()
                let mutable exclGo = exclStep

                while exclGo do
                    hashSet.Add excl.Current |> ignore
                    let! exclStep = excl.MoveNextAsync()
                    exclGo <- exclStep

                while go do
                    let current = e.Current

                    // if true, it was added, and therefore unique, so we return it
                    // if false, it existed, and therefore a duplicate, and we skip
                    if hashSet.Add current then
                        yield current

                    let! step = e.MoveNextAsync()
                    go <- step

        }

    let exceptOfSeq itemsToExclude (source: TaskSeq<_>) =
        checkNonNull (nameof source) source
        checkNonNull (nameof itemsToExclude) itemsToExclude

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step = e.MoveNextAsync()
            go <- step

            if step then
                // only create hashset by the time we actually start iterating;
                // initialize directly from the seq — taskSeq is sequential so no locking needed.
                let hashSet = HashSet<_>(itemsToExclude, HashIdentity.Structural)

                while go do
                    let current = e.Current

                    // if true, it was added, and therefore unique, so we return it
                    // if false, it existed, and therefore a duplicate, and we skip
                    if hashSet.Add current then
                        yield current

                    let! step = e.MoveNextAsync()
                    go <- step

        }

    let distinctUntilChanged (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            let mutable maybePrevious = ValueNone

            for current in source do
                match maybePrevious with
                | ValueNone ->
                    yield current
                    maybePrevious <- ValueSome current
                | ValueSome previous ->
                    if previous = current then
                        () // skip
                    else
                        yield current
                        maybePrevious <- ValueSome current
        }

    let pairwise (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            let mutable maybePrevious = ValueNone

            for current in source do
                match maybePrevious with
                | ValueNone -> maybePrevious <- ValueSome current
                | ValueSome previous ->
                    yield previous, current
                    maybePrevious <- ValueSome current
        }

    let groupBy (projector: ProjectorAction<'T, 'Key, _>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let groups = Dictionary<'Key, ResizeArray<'T>>(HashIdentity.Structural)
            let order = ResizeArray<'Key>()
            let! step = e.MoveNextAsync()
            let mutable go = step

            match projector with
            | ProjectorAction proj ->
                while go do
                    let key = proj e.Current
                    let mutable ra = Unchecked.defaultof<_>

                    if not (groups.TryGetValue(key, &ra)) then
                        ra <- ResizeArray()
                        groups[key] <- ra
                        order.Add key

                    ra.Add e.Current
                    let! step = e.MoveNextAsync()
                    go <- step

            | AsyncProjectorAction proj ->
                while go do
                    let! key = proj e.Current
                    let mutable ra = Unchecked.defaultof<_>

                    if not (groups.TryGetValue(key, &ra)) then
                        ra <- ResizeArray()
                        groups[key] <- ra
                        order.Add key

                    ra.Add e.Current
                    let! step = e.MoveNextAsync()
                    go <- step

            return
                Array.init order.Count (fun i ->
                    let k = order[i]
                    k, groups[k].ToArray())
        }

    let countBy (projector: ProjectorAction<'T, 'Key, _>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let counts = Dictionary<'Key, int>(HashIdentity.Structural)
            let order = ResizeArray<'Key>()
            let! step = e.MoveNextAsync()
            let mutable go = step

            match projector with
            | ProjectorAction proj ->
                while go do
                    let key = proj e.Current
                    let mutable count = 0

                    if not (counts.TryGetValue(key, &count)) then
                        order.Add key

                    counts[key] <- count + 1
                    let! step = e.MoveNextAsync()
                    go <- step

            | AsyncProjectorAction proj ->
                while go do
                    let! key = proj e.Current
                    let mutable count = 0

                    if not (counts.TryGetValue(key, &count)) then
                        order.Add key

                    counts[key] <- count + 1
                    let! step = e.MoveNextAsync()
                    go <- step

            return Array.init order.Count (fun i -> let k = order[i] in k, counts[k])
        }

    let partition (predicate: PredicateAction<'T, _>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let trueItems = ResizeArray<'T>()
            let falseItems = ResizeArray<'T>()
            let! step = e.MoveNextAsync()
            let mutable go = step

            match predicate with
            | Predicate pred ->
                while go do
                    let item = e.Current

                    if pred item then
                        trueItems.Add item
                    else
                        falseItems.Add item

                    let! step = e.MoveNextAsync()
                    go <- step

            | PredicateAsync pred ->
                while go do
                    let item = e.Current
                    let! result = pred item
                    if result then trueItems.Add item else falseItems.Add item
                    let! step = e.MoveNextAsync()
                    go <- step

            return trueItems.ToArray(), falseItems.ToArray()
        }

    let chunkBySize chunkSize (source: TaskSeq<'T>) : TaskSeq<'T[]> =
        if chunkSize < 1 then
            invalidArg (nameof chunkSize) $"The value must be positive, but was %i{chunkSize}."

        checkNonNull (nameof source) source

        taskSeq {
            // Use a fixed-size array with a count index to avoid ResizeArray overhead.
            let buffer = Array.zeroCreate<'T> chunkSize
            let mutable count = 0

            for item in source do
                buffer.[count] <- item
                count <- count + 1

                if count = chunkSize then
                    yield Array.copy buffer
                    count <- 0

            if count > 0 then
                // Last partial chunk: copy only the filled portion.
                yield buffer.[0 .. count - 1]
        }

    let windowed windowSize (source: TaskSeq<_>) =
        if windowSize <= 0 then
            invalidArg (nameof windowSize) $"The value must be positive, but was %i{windowSize}."

        checkNonNull (nameof source) source

        taskSeq {
            // Ring buffer: arr holds elements in circular order.
            // 'count' tracks total elements seen; count % windowSize is the next write position.
            let arr = Array.zeroCreate windowSize
            let mutable count = 0

            for item in source do
                arr.[count % windowSize] <- item
                count <- count + 1

                if count >= windowSize then
                    // Copy ring buffer in source order into a fresh array.
                    let result = Array.zeroCreate windowSize
                    let start = count % windowSize // index of oldest element in the ring

                    if start = 0 then
                        Array.blit arr 0 result 0 windowSize
                    else
                        Array.blit arr start result 0 (windowSize - start)
                        Array.blit arr 0 result (windowSize - start) start

                    yield result
        }
