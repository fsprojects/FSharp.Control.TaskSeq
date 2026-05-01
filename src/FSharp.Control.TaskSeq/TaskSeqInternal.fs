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
type internal ChooserVAction<'T, 'U, 'TaskValueOption when 'TaskValueOption :> Task<'U voption>> =
    | TryPickV of try_pickv: ('T -> 'U voption)
    | TryPickVAsync of async_try_pickv: ('T -> 'TaskValueOption)

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

    let replicate count value =
        raiseCannotBeNegative (nameof count) count

        taskSeq {
            for _ in 1..count do
                yield value
        }

    let replicateInfinite value = taskSeq {
        while true do
            yield value
    }

    let replicateInfiniteAsync (computation: unit -> #Task<'T>) = taskSeq {
        while true do
            let! value = computation ()
            yield value
    }

    let replicateUntilNoneAsync (computation: unit -> #Task<'T option>) = taskSeq {
        let mutable go = true

        while go do
            let! result = computation ()

            match result with
            | Some value -> yield value
            | None -> go <- false
    }

    /// Returns length unconditionally, or based on a predicate
    let lengthBy predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable i = 0

            match predicate with
            | None ->
                while! e.MoveNextAsync() do
                    i <- i + 1

            | Some(Predicate predicate) ->
                while! e.MoveNextAsync() do
                    if predicate e.Current then
                        i <- i + 1

            | Some(PredicateAsync predicate) ->
                while! e.MoveNextAsync() do
                    match! predicate e.Current with
                    | true -> i <- i + 1
                    | false -> ()

            return i
        }

    /// Returns length unconditionally, or based on a predicate
    let lengthBeforeMax max (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable i = 0
            let mutable go = true

            while go && i < max do
                let! hasMore = e.MoveNextAsync()

                if hasMore then i <- i + 1 else go <- false

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

            // Each branch keeps its own while! loop so the match dispatch is hoisted out and
            // the JIT sees a tight, single-case loop (same pattern as sum/sumBy etc.).
            match action with
            | CountableAction action ->
                let mutable i = 0

                while! e.MoveNextAsync() do
                    action i e.Current
                    i <- i + 1

            | SimpleAction action ->
                while! e.MoveNextAsync() do
                    action e.Current

            | AsyncCountableAction action ->
                let mutable i = 0

                while! e.MoveNextAsync() do
                    do! action i e.Current
                    i <- i + 1

            | AsyncSimpleAction action ->
                while! e.MoveNextAsync() do
                    do! action e.Current
        }

    let fold folder initial (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable result = initial

            match folder with
            | FolderAction folder ->
                while! e.MoveNextAsync() do
                    result <- folder result e.Current

            | AsyncFolderAction folder ->
                while! e.MoveNextAsync() do
                    let! tempResult = folder result e.Current
                    result <- tempResult

            return result
        }

    let foldWhile predicate folder initial (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable result = initial
            let mutable running = true

            while running do
                let! hasNext = e.MoveNextAsync()

                if hasNext then
                    if predicate result e.Current then
                        result <- folder result e.Current
                    else
                        running <- false
                else
                    running <- false

            return result
        }

    let foldWhileAsync predicate folder initial (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable result = initial
            let mutable running = true

            while running do
                let! hasNext = e.MoveNextAsync()

                if hasNext then
                    let! keepGoing = predicate result e.Current

                    if keepGoing then
                        let! newState = folder result e.Current
                        result <- newState
                    else
                        running <- false
                else
                    running <- false

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

            match folder with
            | FolderAction folder ->
                while! e.MoveNextAsync() do
                    result <- folder result e.Current

            | AsyncFolderAction folder ->
                while! e.MoveNextAsync() do
                    let! tempResult = folder result e.Current
                    result <- tempResult

            return result
        }

    let mapFold (folder: MapFolderAction<_, _, _, _>) initial (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable state = initial
            let results = ResizeArray()

            match folder with
            | MapFolderAction folder ->
                while! e.MoveNextAsync() do
                    let result, newState = folder state e.Current
                    results.Add result
                    state <- newState

            | AsyncMapFolderAction folder ->
                while! e.MoveNextAsync() do
                    let! (result, newState) = folder state e.Current
                    results.Add result
                    state <- newState

            return results.ToArray(), state
        }

    let threadState (folder: 'State -> 'T -> 'U * 'State) initial (source: TaskSeq<'T>) : TaskSeq<'U> =
        checkNonNull (nameof source) source

        taskSeq {
            let mutable state = initial

            for item in source do
                let result, newState = folder state item
                state <- newState
                yield result
        }

    let threadStateAsync (folder: 'State -> 'T -> #Task<'U * 'State>) initial (source: TaskSeq<'T>) : TaskSeq<'U> =
        checkNonNull (nameof source) source

        taskSeq {
            let mutable state = initial

            for item in source do
                let! (result, newState) = folder state item
                state <- newState
                yield result
        }

    let toResizeArrayAsync (source: TaskSeq<'T>) =
        checkNonNull (nameof source) source

        task {
            let res = ResizeArray<'T>()
            use e = source.GetAsyncEnumerator CancellationToken.None

            while! e.MoveNextAsync() do
                res.Add e.Current

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

    let zip3 (source1: TaskSeq<_>) (source2: TaskSeq<_>) (source3: TaskSeq<_>) =
        checkNonNull (nameof source1) source1
        checkNonNull (nameof source2) source2
        checkNonNull (nameof source3) source3

        taskSeq {
            use e1 = source1.GetAsyncEnumerator CancellationToken.None
            use e2 = source2.GetAsyncEnumerator CancellationToken.None
            use e3 = source3.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step1 = e1.MoveNextAsync()
            let! step2 = e2.MoveNextAsync()
            let! step3 = e3.MoveNextAsync()
            go <- step1 && step2 && step3

            while go do
                yield e1.Current, e2.Current, e3.Current
                let! step1 = e1.MoveNextAsync()
                let! step2 = e2.MoveNextAsync()
                let! step3 = e3.MoveNextAsync()
                go <- step1 && step2 && step3
        }

    let zipWith (mapping: 'T -> 'U -> 'V) (source1: TaskSeq<'T>) (source2: TaskSeq<'U>) =
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
                yield mapping e1.Current e2.Current
                let! step1 = e1.MoveNextAsync()
                let! step2 = e2.MoveNextAsync()
                go <- step1 && step2
        }

    let zipWithAsync (mapping: 'T -> 'U -> #Task<'V>) (source1: TaskSeq<'T>) (source2: TaskSeq<'U>) =
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
                let! result = mapping e1.Current e2.Current
                yield result
                let! step1 = e1.MoveNextAsync()
                let! step2 = e2.MoveNextAsync()
                go <- step1 && step2
        }

    let zipWith3 (mapping: 'T1 -> 'T2 -> 'T3 -> 'V) (source1: TaskSeq<'T1>) (source2: TaskSeq<'T2>) (source3: TaskSeq<'T3>) =
        checkNonNull (nameof source1) source1
        checkNonNull (nameof source2) source2
        checkNonNull (nameof source3) source3

        taskSeq {
            use e1 = source1.GetAsyncEnumerator CancellationToken.None
            use e2 = source2.GetAsyncEnumerator CancellationToken.None
            use e3 = source3.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step1 = e1.MoveNextAsync()
            let! step2 = e2.MoveNextAsync()
            let! step3 = e3.MoveNextAsync()
            go <- step1 && step2 && step3

            while go do
                yield mapping e1.Current e2.Current e3.Current
                let! step1 = e1.MoveNextAsync()
                let! step2 = e2.MoveNextAsync()
                let! step3 = e3.MoveNextAsync()
                go <- step1 && step2 && step3
        }

    let zipWithAsync3 (mapping: 'T1 -> 'T2 -> 'T3 -> #Task<'V>) (source1: TaskSeq<'T1>) (source2: TaskSeq<'T2>) (source3: TaskSeq<'T3>) =
        checkNonNull (nameof source1) source1
        checkNonNull (nameof source2) source2
        checkNonNull (nameof source3) source3

        taskSeq {
            use e1 = source1.GetAsyncEnumerator CancellationToken.None
            use e2 = source2.GetAsyncEnumerator CancellationToken.None
            use e3 = source3.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step1 = e1.MoveNextAsync()
            let! step2 = e2.MoveNextAsync()
            let! step3 = e3.MoveNextAsync()
            go <- step1 && step2 && step3

            while go do
                let! result = mapping e1.Current e2.Current e3.Current
                yield result
                let! step1 = e1.MoveNextAsync()
                let! step2 = e2.MoveNextAsync()
                let! step3 = e3.MoveNextAsync()
                go <- step1 && step2 && step3
        }

    let compareWith (comparer: 'T -> 'T -> int) (source1: TaskSeq<'T>) (source2: TaskSeq<'T>) =
        checkNonNull (nameof source1) source1
        checkNonNull (nameof source2) source2

        task {
            use e1 = source1.GetAsyncEnumerator CancellationToken.None
            use e2 = source2.GetAsyncEnumerator CancellationToken.None
            let mutable result = 0
            let! step1 = e1.MoveNextAsync()
            let! step2 = e2.MoveNextAsync()
            let mutable has1 = step1
            let mutable has2 = step2

            while result = 0 && (has1 || has2) do
                match has1, has2 with
                | false, _ -> result <- -1 // source1 is shorter: less than
                | _, false -> result <- 1 // source2 is shorter: greater than
                | true, true ->
                    let cmp = comparer e1.Current e2.Current

                    if cmp <> 0 then
                        result <- cmp
                    else
                        let! s1 = e1.MoveNextAsync()
                        let! s2 = e2.MoveNextAsync()
                        has1 <- s1
                        has2 <- s2

            return result
        }

    let compareWithAsync (comparer: 'T -> 'T -> #Task<int>) (source1: TaskSeq<'T>) (source2: TaskSeq<'T>) =
        checkNonNull (nameof source1) source1
        checkNonNull (nameof source2) source2

        task {
            use e1 = source1.GetAsyncEnumerator CancellationToken.None
            use e2 = source2.GetAsyncEnumerator CancellationToken.None
            let mutable result = 0
            let! step1 = e1.MoveNextAsync()
            let! step2 = e2.MoveNextAsync()
            let mutable has1 = step1
            let mutable has2 = step2

            while result = 0 && (has1 || has2) do
                match has1, has2 with
                | false, _ -> result <- -1 // source1 is shorter: less than
                | _, false -> result <- 1 // source2 is shorter: greater than
                | true, true ->
                    let! cmp = comparer e1.Current e2.Current

                    if cmp <> 0 then
                        result <- cmp
                    else
                        let! s1 = e1.MoveNextAsync()
                        let! s2 = e2.MoveNextAsync()
                        has1 <- s1
                        has2 <- s2

            return result
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
            let mutable last = ValueNone

            while! e.MoveNextAsync() do
                last <- ValueSome e.Current

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
                        while! e.MoveNextAsync() do
                            yield e.Current
                    }
                    |> Some
        }

    let firstOrDefault defaultValue source =
        tryHead source
        |> Task.map (Option.defaultValue defaultValue)

    let lastOrDefault defaultValue source =
        tryLast source
        |> Task.map (Option.defaultValue defaultValue)

    let splitAt count (source: TaskSeq<'T>) =
        checkNonNull (nameof source) source

        if count < 0 then
            invalidArg (nameof count) $"The value must be non-negative, but was {count}."

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let first = ResizeArray<'T>(count)
            let mutable i = 0
            let mutable go = true

            while go && i < count do
                let! step = e.MoveNextAsync()

                if step then
                    first.Add e.Current
                    i <- i + 1
                else
                    go <- false

            // 'rest' captures 'e' from the outer task block; if the source was not exhausted,
            // advance once past the last element added to 'first', then yield the remainder.
            let rest = taskSeq {
                if go then
                    while! e.MoveNextAsync() do
                        yield e.Current
            }

            return first.ToArray(), rest
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

                // advance past the first `index` elements, then capture the current element
                while go && idx < index do
                    let! step = e.MoveNextAsync()
                    go <- step
                    idx <- idx + 1

                if go then
                    foundItem <- Some e.Current

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

    let chooseV chooser (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {

            match chooser with
            | TryPickV picker ->
                for item in source do
                    match picker item with
                    | ValueSome value -> yield value
                    | ValueNone -> ()

            | TryPickVAsync picker ->
                for item in source do
                    match! picker item with
                    | ValueSome value -> yield value
                    | ValueNone -> ()
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
                    let mutable i = 0
                    let mutable cont = true

                    // advance past 'count' elements; stop early if the source is shorter
                    while cont && i < count do
                        let! hasMore = e.MoveNextAsync()
                        if hasMore then i <- i + 1 else cont <- false

                    // return remaining elements; enumerator is at element (count-1) so one
                    // more MoveNext is needed to reach element (count)
                    if cont then
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

                    for _ in count .. -1 .. 1 do
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
                    let mutable yielded = 0
                    let mutable cont = true

                    // yield up to 'count' elements; stop when exhausted or limit reached
                    while cont && yielded < count do
                        let! hasMore = e.MoveNextAsync()

                        if hasMore then
                            yield e.Current
                            yielded <- yielded + 1
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
        checkNonNull (nameof source) source

        match valueOrValues with
        | Many values -> checkNonNull "values" values
        | One _ -> ()

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
        checkNonNull (nameof source) source
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
        checkNonNull (nameof source) source
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
        checkNonNull (nameof source) source
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
            let! hasFirst = e.MoveNextAsync()

            if hasFirst then
                // only create hashset by the time we actually start iterating;
                // taskSeq enumerates sequentially, so a plain HashSet suffices — no locking needed.
                let hashSet = HashSet<_>(HashIdentity.Structural)

                use excl = itemsToExclude.GetAsyncEnumerator CancellationToken.None

                while! excl.MoveNextAsync() do
                    hashSet.Add excl.Current |> ignore

                // if true, it was added, and therefore unique, so we return it
                // if false, it existed, and therefore a duplicate, and we skip
                if hashSet.Add e.Current then
                    yield e.Current

                while! e.MoveNextAsync() do
                    let current = e.Current

                    if hashSet.Add current then
                        yield current

        }

    let exceptOfSeq itemsToExclude (source: TaskSeq<_>) =
        checkNonNull (nameof source) source
        checkNonNull (nameof itemsToExclude) itemsToExclude

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! hasFirst = e.MoveNextAsync()

            if hasFirst then
                // only create hashset by the time we actually start iterating;
                // initialize directly from the seq — taskSeq is sequential so no locking needed.
                let hashSet = HashSet<_>(itemsToExclude, HashIdentity.Structural)

                // if true, it was added, and therefore unique, so we return it
                // if false, it existed, and therefore a duplicate, and we skip
                if hashSet.Add e.Current then
                    yield e.Current

                while! e.MoveNextAsync() do
                    let current = e.Current

                    if hashSet.Add current then
                        yield current

        }

    let distinctUntilChanged (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! hasFirst = e.MoveNextAsync()

            if hasFirst then
                let mutable previous = e.Current
                yield previous

                while! e.MoveNextAsync() do
                    let current = e.Current

                    if current <> previous then
                        yield current
                        previous <- current
        }

    let distinctUntilChangedWith (comparer: 'T -> 'T -> bool) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! hasFirst = e.MoveNextAsync()

            if hasFirst then
                let mutable previous = e.Current
                yield previous

                while! e.MoveNextAsync() do
                    let current = e.Current

                    if not (comparer previous current) then
                        yield current
                        previous <- current
        }

    let distinctUntilChangedWithAsync (comparer: 'T -> 'T -> #Task<bool>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! hasFirst = e.MoveNextAsync()

            if hasFirst then
                let mutable previous = e.Current
                yield previous

                while! e.MoveNextAsync() do
                    let current = e.Current
                    let! areEqual = comparer previous current

                    if not areEqual then
                        yield current
                        previous <- current
        }

    let pairwise (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! hasFirst = e.MoveNextAsync()

            if hasFirst then
                let mutable previous = e.Current

                while! e.MoveNextAsync() do
                    let current = e.Current
                    yield previous, current
                    previous <- current
        }

    let groupBy (projector: ProjectorAction<'T, 'Key, _>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let groups = Dictionary<'Key, ResizeArray<'T>>(HashIdentity.Structural)
            let order = ResizeArray<'Key>()

            match projector with
            | ProjectorAction proj ->
                while! e.MoveNextAsync() do
                    let key = proj e.Current
                    let mutable ra = Unchecked.defaultof<_>

                    if not (groups.TryGetValue(key, &ra)) then
                        ra <- ResizeArray()
                        groups[key] <- ra
                        order.Add key

                    ra.Add e.Current

            | AsyncProjectorAction proj ->
                while! e.MoveNextAsync() do
                    let! key = proj e.Current
                    let mutable ra = Unchecked.defaultof<_>

                    if not (groups.TryGetValue(key, &ra)) then
                        ra <- ResizeArray()
                        groups[key] <- ra
                        order.Add key

                    ra.Add e.Current

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

            match projector with
            | ProjectorAction proj ->
                while! e.MoveNextAsync() do
                    let key = proj e.Current
                    let mutable count = 0

                    if not (counts.TryGetValue(key, &count)) then
                        order.Add key

                    counts[key] <- count + 1

            | AsyncProjectorAction proj ->
                while! e.MoveNextAsync() do
                    let! key = proj e.Current
                    let mutable count = 0

                    if not (counts.TryGetValue(key, &count)) then
                        order.Add key

                    counts[key] <- count + 1

            return Array.init order.Count (fun i -> let k = order[i] in k, counts[k])
        }

    let partition (predicate: PredicateAction<'T, _>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let trueItems = ResizeArray<'T>()
            let falseItems = ResizeArray<'T>()

            match predicate with
            | Predicate pred ->
                while! e.MoveNextAsync() do
                    let item = e.Current

                    if pred item then
                        trueItems.Add item
                    else
                        falseItems.Add item

            | PredicateAsync pred ->
                while! e.MoveNextAsync() do
                    let item = e.Current
                    let! result = pred item
                    if result then trueItems.Add item else falseItems.Add item

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

    let chunkBy (projection: 'T -> 'Key) (source: TaskSeq<'T>) : TaskSeq<'Key * 'T[]> =
        checkNonNull (nameof source) source

        taskSeq {
            let mutable maybeCurrentKey = ValueNone
            let mutable currentChunk = ResizeArray<'T>()

            for item in source do
                let key = projection item

                match maybeCurrentKey with
                | ValueNone ->
                    maybeCurrentKey <- ValueSome key
                    currentChunk.Add item
                | ValueSome prevKey ->
                    if prevKey = key then
                        currentChunk.Add item
                    else
                        yield prevKey, currentChunk.ToArray()
                        currentChunk.Clear() // reuse backing array; ToArray() already captured a snapshot
                        currentChunk.Add item
                        maybeCurrentKey <- ValueSome key

            match maybeCurrentKey with
            | ValueNone -> ()
            | ValueSome lastKey -> yield lastKey, currentChunk.ToArray()
        }

    let chunkByAsync (projection: 'T -> #Task<'Key>) (source: TaskSeq<'T>) : TaskSeq<'Key * 'T[]> =
        checkNonNull (nameof source) source

        taskSeq {
            let mutable maybeCurrentKey = ValueNone
            let mutable currentChunk = ResizeArray<'T>()

            for item in source do
                let! key = projection item

                match maybeCurrentKey with
                | ValueNone ->
                    maybeCurrentKey <- ValueSome key
                    currentChunk.Add item
                | ValueSome prevKey ->
                    if prevKey = key then
                        currentChunk.Add item
                    else
                        yield prevKey, currentChunk.ToArray()
                        currentChunk.Clear() // reuse backing array; ToArray() already captured a snapshot
                        currentChunk.Add item
                        maybeCurrentKey <- ValueSome key

            match maybeCurrentKey with
            | ValueNone -> ()
            | ValueSome lastKey -> yield lastKey, currentChunk.ToArray()
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
